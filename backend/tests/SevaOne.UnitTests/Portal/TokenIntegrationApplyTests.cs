using FluentAssertions;
using Moq;
using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Services;
using SevaOne.Business.SevaSindhu.Interfaces;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.UnitTests.Portal;

/// <summary>The citizen apply click for a service with an enabled token integration (Seva Sindhu V3 handoff).</summary>
public class TokenIntegrationApplyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private const string Landing = "https://dept.example.gov.in/apply";

    private readonly Guid _citizenId = Guid.NewGuid();
    private readonly Mock<IPortalRepository> _portal = new();
    private readonly Mock<IDiscoveryRepository> _discovery = new();
    private readonly Mock<IAuditRepository> _audit = new();
    private readonly Mock<ICurrentCitizen> _current = new();
    private readonly Mock<IJwsTokenService> _jws = new();
    private readonly Mock<IApplyRedirectProtector> _protector = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ITokenIntegrationRepository> _integrations = new();

    private readonly Scheme _scheme = new() { Id = Guid.NewGuid(), ServiceCode = "2971", DepartmentId = Guid.NewGuid(), Name = "Renewal of recognition", DepartmentServiceId = "341", IsActive = true };
    private readonly Department _department = new() { Id = Guid.NewGuid(), Code = "education", Name = "Education Department", IsActive = true };
    private readonly Citizen _citizen = new() { SevaOneUserId = 4242, Mobile = "9000000099", DigiLockerId = "DL-1", Email = "a@b.in" };

    private readonly Mock<SevaOne.Business.Discovery.Interfaces.IKaveriSsoService> _kaveri = new();
    private TokenIntegrationAttemptWrite? _attempt;
    private SevaOneToken? _token;

    public TokenIntegrationApplyTests()
    {
        _citizen.Id = _citizenId;
        _scheme.DepartmentId = _department.Id;
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _current.SetupGet(c => c.CitizenId).Returns(_citizenId);
        _discovery.Setup(d => d.GetSchemeByDepartmentAndServiceCodeAsync("education", "2971", It.IsAny<CancellationToken>())).ReturnsAsync(_scheme);
        _discovery.Setup(d => d.GetDepartmentByIdAsync(_department.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_department);
        _portal.Setup(p => p.GetCitizenByIdAsync(_citizenId, It.IsAny<CancellationToken>())).ReturnsAsync(_citizen);
        _portal.Setup(p => p.InsertTokenAsync(It.IsAny<SevaOneToken>(), It.IsAny<SevaOne.Infrastructure.Repositories.Common.IUnitOfWork?>(), It.IsAny<CancellationToken>()))
            .Callback<SevaOneToken, SevaOne.Infrastructure.Repositories.Common.IUnitOfWork?, CancellationToken>((t, _, _) => _token = t)
            .Returns(Task.CompletedTask);
        _jws.Setup(j => j.Mint(It.IsAny<JwsClaims>())).Returns(("jws-plain", "jws-hash"));
        _protector.Setup(p => p.Protect("jws-plain")).Returns("jws-encrypted");
        _integrations.Setup(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()))
            .Callback<TokenIntegrationAttemptWrite, CancellationToken>((a, _) => _attempt = a)
            .ReturnsAsync("EDCS100100000001");
    }

    private PortalApplyService Service() => new(
        _portal.Object, _discovery.Object, _audit.Object, _current.Object, Mock.Of<ICurrentOperator>(), _jws.Object, _protector.Object,
        Mock.Of<IAjskRedirectService>(), Mock.Of<ISevaSindhuCatalog2Service>(), Mock.Of<ISevaSindhuEscomService>(), _clock.Object, _integrations.Object, _kaveri.Object,
        Mock.Of<SevaOne.Business.Discovery.Interfaces.IEdcsApiTokenService>(), Mock.Of<SevaOne.Business.Discovery.Interfaces.IEdcsApiFormTokenService>());

    private void EnableIntegration(bool sendMobile = false, bool sendDigilocker = false) =>
        _integrations.Setup(i => i.GetEnabledAsync(_scheme.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenIntegrationActiveRow { TypeKey = "seva_sindhu_v3", LandingUrl = Landing, SendMobile = sendMobile, SendDigilockerId = sendDigilocker });

    [Fact]
    public async Task A_citizen_with_no_email_is_asked_once_and_it_is_stored()
    {
        EnableIntegration();
        _citizen.Email = null;

        var first = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);
        first.NeedsInput.Should().ContainSingle().Which.Key.Should().Be("Email");
        _portal.Verify(p => p.UpdateCitizenAsync(It.IsAny<Citizen>(), It.IsAny<CancellationToken>()), Times.Never);

        var bad = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971", new Dictionary<string, string> { ["Email"] = "nope" }), CancellationToken.None);
        bad.NeedsInput.Should().NotBeNull();

        var done = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971", new Dictionary<string, string> { ["Email"] = "me@x.in" }), CancellationToken.None);
        done.NeedsInput.Should().BeNull();
        _citizen.Email.Should().Be("me@x.in");
        _portal.Verify(p => p.UpdateCitizenAsync(_citizen, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logged_in_citizen_gets_a_form_post_with_the_contract_fields()
    {
        EnableIntegration();

        var outcome = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        outcome.RequiresLogin.Should().BeFalse();
        outcome.ApplyUrl.Should().BeNull();
        outcome.ExpiresAt.Should().Be(Now.AddMinutes(30));
        outcome.PostForm.Should().NotBeNull();
        outcome.PostForm!.Url.Should().Be(Landing);
        outcome.PostForm.Fields.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["UserId"] = "4242",
            ["assistedmode"] = "false",
            ["ServiceId"] = "341",
            ["edcsApplicationRefNo"] = "EDCS100100000001",
            ["Token"] = "jws-encrypted",
        });
    }

    [Fact]
    public async Task Mobile_and_digilocker_are_sent_only_when_the_setup_asks_for_them()
    {
        EnableIntegration(sendMobile: true, sendDigilocker: true);

        var outcome = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        outcome.PostForm!.Fields.Should().Contain("mobile", "9000000099").And.Contain("digilockerid", "DL-1");
    }

    [Fact]
    public async Task Service_id_falls_back_to_our_code_when_the_department_has_none()
    {
        _scheme.DepartmentServiceId = null;
        EnableIntegration();

        var outcome = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        outcome.PostForm!.Fields["ServiceId"].Should().Be("2971");
    }

    [Fact]
    public async Task The_attempt_and_token_are_recorded_and_the_token_is_not_stored_in_the_sent_fields()
    {
        EnableIntegration();

        await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        _token.Should().NotBeNull();
        _token!.TokenHash.Should().Be("jws-hash");
        _token.AssistedMode.Should().BeFalse();
        _token.ExpireAt.Should().Be(Now.AddMinutes(30));
        _attempt.Should().NotBeNull();
        _attempt!.TokenId.Should().Be(_token.Id);
        _attempt.LandingUrl.Should().Be(Landing);
        _attempt.SentFields.Should().NotContain("jws").And.NotContain("Token").And.NotContain("edcsApplicationRefNo");
        _audit.Verify(a => a.InsertAsync(It.Is<AuditLog>(l => l.Action == "apply.token_integration.started" && l.Detail.Contains("EDCS100100000001")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Anonymous_visitor_is_asked_to_sign_in_and_nothing_is_minted()
    {
        _current.SetupGet(c => c.CitizenId).Returns((Guid?)null);
        EnableIntegration();

        var outcome = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        outcome.RequiresLogin.Should().BeTrue();
        outcome.PostForm.Should().BeNull();
        _jws.Verify(j => j.Mint(It.IsAny<JwsClaims>()), Times.Never);
        _integrations.Verify(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_service_without_an_enabled_setup_keeps_the_existing_behaviour()
    {
        _integrations.Setup(i => i.GetEnabledAsync(_scheme.Id, It.IsAny<CancellationToken>())).ReturnsAsync((TokenIntegrationActiveRow?)null);
        _department.IsIntegrated = false;
        _scheme.DeeplinkCitizenUrl = "https://normal.example.gov.in/apply";

        var outcome = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        outcome.PostForm.Should().BeNull();
        outcome.ApplyUrl.Should().Be("https://normal.example.gov.in/apply");
        outcome.IsHomepageFallback.Should().BeTrue();
        _integrations.Verify(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_single_sign_on_service_gives_the_citizen_the_address_the_other_system_returned()
    {
        var integration = new TokenIntegrationActiveRow { TypeKey = "kaveri_sso", ExtraConfig = "{}" };
        _integrations.Setup(i => i.GetEnabledAsync(_scheme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(integration);
        _kaveri.Setup(k => k.StartAsync(_citizen, _scheme, _department, integration, It.IsAny<CancellationToken>())).ReturnsAsync("https://kaveri.example.gov.in/authenticate?id=1");

        var outcome = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        outcome.ApplyUrl.Should().Be("https://kaveri.example.gov.in/authenticate?id=1");
        outcome.PostForm.Should().BeNull();
        outcome.RequiresLogin.Should().BeFalse();
        _jws.Verify(j => j.Mint(It.IsAny<JwsClaims>()), Times.Never); // no SevaOne token is involved
    }

    [Fact]
    public async Task A_single_sign_on_service_asks_an_anonymous_visitor_to_sign_in_first()
    {
        _current.SetupGet(c => c.CitizenId).Returns((Guid?)null);
        _integrations.Setup(i => i.GetEnabledAsync(_scheme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new TokenIntegrationActiveRow { TypeKey = "kaveri_sso" });

        var outcome = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        outcome.RequiresLogin.Should().BeTrue();
        _kaveri.Verify(k => k.StartAsync(It.IsAny<Citizen>(), It.IsAny<Scheme>(), It.IsAny<Department>(), It.IsAny<TokenIntegrationActiveRow>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_type_the_apply_flow_does_not_know_is_ignored_and_the_normal_link_is_used()
    {
        _integrations.Setup(i => i.GetEnabledAsync(_scheme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new TokenIntegrationActiveRow { TypeKey = "future_type" });
        _department.IsIntegrated = false;
        _scheme.DeeplinkCitizenUrl = "https://normal.example.gov.in/apply";

        var outcome = await Service().CreateApplyTokenAsync(new CreateApplyTokenRequest("education", "2971"), CancellationToken.None);

        outcome.ApplyUrl.Should().Be("https://normal.example.gov.in/apply");
    }
}
