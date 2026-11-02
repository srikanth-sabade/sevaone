using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Moq;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Business.Discovery.Services;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.UnitTests.Portal;

/// <summary>The department-side token check (what a department calls when the citizen lands on its page).</summary>
public class TokenVerificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);
    private const string Ref = "EDCS100100000001";
    private readonly Guid _departmentId = Guid.NewGuid();
    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<IApplyRedirectProtector> _protector = new();
    private readonly Mock<IClock> _clock = new();
    private readonly TokenVerifyInfoRow _info;
    private readonly TokenIntegrationCaller _caller;

    public TokenVerificationTests()
    {
        _info = new TokenVerifyInfoRow
        {
            AttemptId = Guid.NewGuid(), EdcsApplicationRefNo = Ref, DepartmentId = _departmentId, DepartmentCode = "education", SevaOneUserId = 4242,
            ExpiresAt = Now.AddMinutes(20), TokenHash = Sha("jws-plain"), CitizenName = "Asha Rao", CitizenMobile = "9000000099", CitizenDigilockerId = "DL-1",
            ServiceValue = "341",
        };
        _caller = new TokenIntegrationCaller(Guid.NewGuid(), _departmentId, "education");
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _integrations.Setup(i => i.GetVerifyInfoAsync(Ref, It.IsAny<CancellationToken>())).ReturnsAsync(_info);
        _integrations.Setup(i => i.MarkTokenVerifiedAsync(_info.AttemptId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _protector.Setup(p => p.TryUnprotect("jws-encrypted")).Returns("jws-plain");
    }

    private static string Sha(string v) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(v))).ToLowerInvariant();
    private TokenVerificationService Service() => new(_integrations.Object, _protector.Object, _clock.Object);
    private static TokenVerifyRequest Req(string? token = "jws-encrypted", string? reference = Ref) => new() { Token = token, EdcsApplicationRefNo = reference };

    [Fact]
    public async Task A_genuine_token_is_confirmed_with_the_user()
    {
        var result = await Service().VerifyAsync(Req(), _caller, CancellationToken.None);

        result.Valid.Should().BeTrue();
        result.UserId.Should().Be("4242");
        result.Name.Should().Be("Asha Rao");
        result.ServiceId.Should().Be("341");
        result.AssistedMode.Should().BeFalse();
        result.AlreadyVerified.Should().BeFalse();
        result.Mobile.Should().BeNull();        // the service is not set to share these
        result.DigilockerId.Should().BeNull();
    }

    [Fact]
    public async Task Mobile_and_digilocker_are_given_only_when_the_service_shares_them()
    {
        _info.SendMobile = true;
        _info.SendDigilockerId = true;

        var result = await Service().VerifyAsync(Req(), _caller, CancellationToken.None);

        result.Mobile.Should().Be("9000000099");
        result.DigilockerId.Should().Be("DL-1");
    }

    [Fact]
    public async Task A_second_check_still_works_and_says_it_is_a_repeat()
    {
        _integrations.Setup(i => i.MarkTokenVerifiedAsync(_info.AttemptId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        (await Service().VerifyAsync(Req(), _caller, CancellationToken.None)).AlreadyVerified.Should().BeTrue();
    }

    [Theory]
    [InlineData("forged", Ref, "token_mismatch")]
    [InlineData("jws-encrypted", "EDCS999", "unknown_application")]
    [InlineData(null, Ref, "missing_fields")]
    [InlineData("jws-encrypted", null, "missing_fields")]
    public async Task A_bad_request_is_refused_with_a_reason_and_no_user_details(string? token, string? reference, string reason)
    {
        _integrations.Setup(i => i.GetVerifyInfoAsync("EDCS999", It.IsAny<CancellationToken>())).ReturnsAsync((TokenVerifyInfoRow?)null);

        var result = await Service().VerifyAsync(Req(token, reference), _caller, CancellationToken.None);

        result.Valid.Should().BeFalse();
        result.Reason.Should().Be(reason);
        result.Name.Should().BeNull();
        result.UserId.Should().BeNull();
    }

    [Fact]
    public async Task Another_departments_application_is_refused()
    {
        var other = _caller with { DepartmentId = Guid.NewGuid() };

        var result = await Service().VerifyAsync(Req(), other, CancellationToken.None);

        result.Should().Be(new TokenVerifyResult(false, "wrong_department"));
        _integrations.Verify(i => i.MarkTokenVerifiedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task An_expired_or_revoked_token_is_refused()
    {
        _info.ExpiresAt = Now.AddMinutes(-1);
        (await Service().VerifyAsync(Req(), _caller, CancellationToken.None)).Reason.Should().Be("token_expired");

        _info.ExpiresAt = Now.AddMinutes(20);
        _info.TokenRevoked = true;
        (await Service().VerifyAsync(Req(), _caller, CancellationToken.None)).Reason.Should().Be("token_revoked");
    }
}

/// <summary>Practice (sandbox) applications and the test calls a department can make from the Department Guide.</summary>
public class DeptPortalServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _departmentId = Guid.NewGuid();
    private readonly Mock<IDiscoveryRepository> _discovery = new();
    private readonly Mock<IPortalRepository> _portal = new();
    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<IJwsTokenService> _jws = new();
    private readonly Mock<IApplyRedirectProtector> _protector = new();
    private readonly Mock<ITokenStatusHttpClient> _http = new();
    private readonly Mock<ITokenVerificationService> _verification = new();
    private readonly Mock<ITokenAcknowledgementService> _acks = new();
    private readonly Mock<IClock> _clock = new();
    private readonly TokenIntegrationCaller _caller;
    private readonly Scheme _scheme = new() { Id = Guid.NewGuid(), ServiceCode = "2971", DepartmentServiceId = "341", IsActive = true, Name = "Renewal" };
    private TokenIntegrationAttemptWrite? _attempt;
    private SevaOneToken? _token;

    public DeptPortalServiceTests()
    {
        _caller = new TokenIntegrationCaller(Guid.NewGuid(), _departmentId, "education");
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _discovery.Setup(d => d.GetDepartmentByCodeAsync("education", It.IsAny<CancellationToken>())).ReturnsAsync(new Department { Id = _departmentId, Code = "education" });
        _discovery.Setup(d => d.GetSchemeByCodeAsync(_departmentId, "2971", It.IsAny<CancellationToken>())).ReturnsAsync(_scheme);
        _discovery.Setup(d => d.ListSchemesAsync(_departmentId, It.IsAny<CancellationToken>())).ReturnsAsync([_scheme]);
        _portal.Setup(p => p.GetCitizenByWso2SubjectAsync("test-citizen-dept-integration-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Citizen { Id = Guid.NewGuid(), SevaOneUserId = 1, Name = "Test Citizen One" });
        _portal.Setup(p => p.InsertTokenAsync(It.IsAny<SevaOneToken>(), It.IsAny<SevaOne.Infrastructure.Repositories.Common.IUnitOfWork?>(), It.IsAny<CancellationToken>()))
            .Callback<SevaOneToken, SevaOne.Infrastructure.Repositories.Common.IUnitOfWork?, CancellationToken>((t, _, _) => _token = t).Returns(Task.CompletedTask);
        _jws.Setup(j => j.Mint(It.IsAny<JwsClaims>())).Returns(("jws-plain", "jws-hash"));
        _protector.Setup(p => p.Protect("jws-plain")).Returns("jws-encrypted");
        _integrations.Setup(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()))
            .Callback<TokenIntegrationAttemptWrite, CancellationToken>((a, _) => _attempt = a).ReturnsAsync("EDCSTEST100100000001");
    }

    private DeptPortalService Service() => new(_discovery.Object, _portal.Object, _integrations.Object, _jws.Object, _protector.Object,
        _http.Object, _verification.Object, _acks.Object, _clock.Object);

    [Fact]
    public async Task A_practice_application_uses_the_test_citizen_and_is_marked_sandbox()
    {
        var start = await Service().StartSandboxAsync(_caller, "2971", CancellationToken.None);

        start.EdcsApplicationRefNo.Should().StartWith("EDCSTEST");
        start.Fields.Should().Contain("UserId", "1").And.Contain("ServiceId", "341").And.Contain("assistedmode", "false")
            .And.Contain("Token", "jws-encrypted").And.Contain("edcsApplicationRefNo", "EDCSTEST100100000001");
        _attempt!.IsSandbox.Should().BeTrue();
        _attempt.SentFields.Should().NotContain("jws");
        _token!.TokenHash.Should().Be("jws-hash");
        _token.ExpireAt.Should().Be(Now.AddMinutes(30));
    }

    [Fact]
    public async Task Each_practice_token_carries_its_own_marker_so_two_in_one_second_do_not_clash()
    {
        var markers = new List<string?>();
        _jws.Setup(j => j.Mint(It.IsAny<JwsClaims>())).Callback<JwsClaims>(c => markers.Add(c.OperatorId)).Returns(("jws-plain", "jws-hash"));

        await Service().StartSandboxAsync(_caller, "2971", CancellationToken.None);
        await Service().StartSandboxAsync(_caller, "2971", CancellationToken.None);

        markers.Should().HaveCount(2).And.OnlyContain(m => m != null && m.StartsWith("sandbox-"));
        markers[0].Should().NotBe(markers[1]);
    }

    [Fact]
    public async Task Without_a_service_code_the_first_active_service_is_used_and_a_foreign_one_is_refused()
    {
        (await Service().StartSandboxAsync(_caller, null, CancellationToken.None)).ServiceCode.Should().Be("2971");

        _discovery.Setup(d => d.GetSchemeByCodeAsync(_departmentId, "9999", It.IsAny<CancellationToken>())).ReturnsAsync((Scheme?)null);
        var act = () => Service().StartSandboxAsync(_caller, "9999", CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Optional_fields_appear_only_when_the_service_sends_them_and_carry_made_up_values()
    {
        _integrations.Setup(i => i.GetAsync(_scheme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new TokenIntegrationRow { SendMobile = true, SendDigilockerId = true });

        var start = await Service().StartSandboxAsync(_caller, "2971", CancellationToken.None);

        start.Fields.Should().Contain("mobile", "9000000000").And.Contain("digilockerid", "DL-SANDBOX");
    }

    [Fact]
    public async Task The_landing_test_posts_the_fields_and_reports_the_status()
    {
        IReadOnlyDictionary<string, string>? sent = null;
        _http.Setup(h => h.PostFormAsync("https://dept.example.gov.in/land", It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, string>, CancellationToken>((_, f, _) => sent = f).ReturnsAsync(new TokenStatusHttpResult(200, "<html/>", null));

        var result = await Service().TestLandingAsync(_caller, "https://dept.example.gov.in/land", "2971", CancellationToken.None);

        result.Ok.Should().BeTrue();
        result.HttpStatus.Should().Be(200);
        result.FieldsSent.Should().Contain("Token").And.Contain("edcsApplicationRefNo");
        sent!["Token"].Should().Be("jws-encrypted");
    }

    [Theory]
    [InlineData(302, false)]
    [InlineData(500, false)]
    public async Task Anything_but_200_from_the_landing_page_is_not_ok(int status, bool ok)
    {
        _http.Setup(h => h.PostFormAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TokenStatusHttpResult(status, null, null));

        var result = await Service().TestLandingAsync(_caller, "https://dept.example.gov.in/land", null, CancellationToken.None);

        result.Ok.Should().Be(ok);
        result.Note.Should().Contain(status.ToString());
    }

    [Fact]
    public async Task An_unreachable_landing_page_says_so()
    {
        _http.Setup(h => h.PostFormAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TokenStatusHttpResult(null, null, "Could not reach it."));

        var result = await Service().TestLandingAsync(_caller, "https://dept.example.gov.in/land", null, CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Error.Should().Be("Could not reach it.");
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://dept.example.gov.in/x")]
    [InlineData("https://user:pw@dept.example.gov.in/x")]
    [InlineData("")]
    public async Task A_landing_address_that_is_not_a_plain_web_address_is_refused(string url)
    {
        var act = () => Service().TestLandingAsync(_caller, url, null, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>();
        _http.Verify(h => h.PostFormAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_status_test_reads_the_status_from_the_reply()
    {
        string? body = null;
        _http.Setup(h => h.PostJsonAsync("https://dept.example.gov.in/status", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TokenStatusAuth, CancellationToken>((_, b, _, _) => body = b)
            .ReturnsAsync(new TokenStatusHttpResult(200, """{"data":{"state":"Approved","note":"Done"}}""", null));

        var result = await Service().TestStatusAsync(new StatusTestRequest
        {
            Url = "https://dept.example.gov.in/status", Number = "GSC-1", RequestField = "GscNumber", StatusPath = "data.state", MessagePath = "data.note",
        }, CancellationToken.None);

        body.Should().Be("""{"GscNumber":"GSC-1"}""");
        result.DepartmentStatus.Should().Be("Approved");
        result.Message.Should().Be("Done");
        result.Error.Should().BeNull();
    }

    [Theory]
    [InlineData(500, "{}", "HTTP 500")]
    [InlineData(200, "not json", "not JSON")]
    [InlineData(200, """{"other":1}""", "No status was found")]
    public async Task The_status_test_explains_what_is_wrong(int status, string reply, string expected)
    {
        _http.Setup(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TokenStatusHttpResult(status, reply, null));

        var result = await Service().TestStatusAsync(new StatusTestRequest { Url = "https://dept.example.gov.in/status", Number = "1" }, CancellationToken.None);

        result.Error.Should().Contain(expected);
    }

    [Theory]
    [InlineData(null, "1", null)]
    [InlineData("https://x.gov.in/s", null, null)]
    [InlineData("https://x.gov.in/s", "1", "bad field!")]
    public async Task A_status_test_with_missing_or_odd_input_is_refused(string? url, string? number, string? field)
    {
        var act = () => Service().TestStatusAsync(new StatusTestRequest { Url = url, Number = number, RequestField = field }, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Sandbox_checks_and_acknowledgements_refuse_real_applications()
    {
        _integrations.Setup(i => i.GetVerifyInfoAsync("EDCS1", It.IsAny<CancellationToken>())).ReturnsAsync(new TokenVerifyInfoRow { IsSandbox = false });
        _integrations.Setup(i => i.GetAttemptAsync("EDCS1", It.IsAny<CancellationToken>())).ReturnsAsync(new TokenIntegrationAttemptRow { IsSandbox = false });

        (await Service().VerifySandboxAsync(new TokenVerifyRequest { EdcsApplicationRefNo = "EDCS1", Token = "t" }, _caller, CancellationToken.None))
            .Reason.Should().Be("not_a_sandbox_application");
        var (outcome, notSandbox) = await Service().AcknowledgeSandboxAsync(new TokenAckRequest { EdcsApplicationRefNo = "EDCS1" }, _caller, null, CancellationToken.None);

        notSandbox.Should().BeTrue();
        outcome.IsSuccess.Should().BeFalse();
        _acks.Verify(a => a.AcknowledgeAsync(It.IsAny<TokenAckRequest>(), It.IsAny<TokenIntegrationCaller>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
