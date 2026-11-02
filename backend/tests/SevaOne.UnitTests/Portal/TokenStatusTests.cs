using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using SevaOne.Business.Common;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Services;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace SevaOne.UnitTests.Portal;

/// <summary>Reading a department's status reply, and asking for it (token integration, URL 3).</summary>
public class TokenStatusJsonTests
{
    private static string? Read(string json, string? path) => TokenStatusJson.ReadString(JsonDocument.Parse(json).RootElement, path);

    [Theory]
    [InlineData("""{"status":"Approved"}""", "status", "Approved")]
    [InlineData("""{"Status":"Approved"}""", "status", "Approved")]
    [InlineData("""{"data":{"state":"Pending"}}""", "data.state", "Pending")]
    [InlineData("""{"data":{"state":"Pending"}}""", "$.data.state", "Pending")]
    [InlineData("""{"result":[{"status":"A"},{"status":"B"}]}""", "result[1].status", "B")]
    [InlineData("""{"code":3}""", "code", "3")]
    [InlineData("""{"done":true}""", "done", "true")]
    public void A_value_is_found_by_its_path(string json, string path, string expected) => Read(json, path).Should().Be(expected);

    [Theory]
    [InlineData("""{"status":"x"}""", "missing")]
    [InlineData("""{"status":{"a":1}}""", "status")]
    [InlineData("""{"status":null}""", "status")]
    [InlineData("""{"result":[]}""", "result[0].status")]
    [InlineData("""{"status":"x"}""", "status.deeper")]
    [InlineData("""{"status":"x"}""", "")]
    public void Anything_else_gives_nothing(string json, string path) => Read(json, path).Should().BeNull();

    [Fact]
    public void The_departments_word_maps_to_our_status_ignoring_case_and_spaces()
    {
        var config = new TokenStatusConfig(StatusMap: new Dictionary<string, string> { ["In Progress"] = "UnderReview", ["APPROVED"] = "approved" });

        TokenStatusJson.Map(config, "  in progress ").Should().Be(ApplicationStatus.UnderReview);
        TokenStatusJson.Map(config, "Approved").Should().Be(ApplicationStatus.Approved);
        TokenStatusJson.Map(config, "Something else").Should().BeNull();
        TokenStatusJson.Map(new TokenStatusConfig(), "Approved").Should().BeNull();
    }

    [Fact]
    public void Saved_settings_round_trip_and_damaged_ones_give_the_defaults()
    {
        var config = new TokenStatusConfig("basic", "svc", null, "data.state", "message", new Dictionary<string, string> { ["Done"] = "Approved" });

        TokenStatusJson.Read(TokenStatusJson.Write(config)).Should().BeEquivalentTo(config);
        TokenStatusJson.Read("not json").Should().Be(new TokenStatusConfig());
        TokenStatusJson.Read(null).StatusPath.Should().Be("status");
        TokenStatusJson.Read("{}").AuthType.Should().Be("none");
    }
}

public class OutboundAddressGuardTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("::1")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    [InlineData("::ffff:10.0.0.1")]
    public void Internal_addresses_are_blocked(string ip) => OutboundAddressGuard.IsBlocked(IPAddress.Parse(ip)).Should().BeTrue();

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("164.100.80.180")]
    [InlineData("103.21.4.10")]
    [InlineData("172.32.0.1")]
    [InlineData("2606:4700:4700::1111")]
    public void Public_addresses_are_allowed(string ip) => OutboundAddressGuard.IsBlocked(IPAddress.Parse(ip)).Should().BeFalse();
}

public class TokenStatusServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private const string EdcsRef = "EDCS100100000001";

    private readonly Guid _citizenId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<IDiscoveryRepository> _discovery = new();
    private readonly Mock<IOrchestrationRepository> _orchestration = new();
    private readonly Mock<IStatusRepository> _status = new();
    private readonly Mock<ITokenStatusHttpClient> _http = new();
    private readonly Mock<ITokenStatusSecretProtector> _secrets = new();
    private readonly Mock<ICurrentCitizen> _current = new();
    private readonly Mock<IClock> _clock = new();
    private readonly TokenStatusTargetRow _target;
    private string? _sentBody;
    private TokenStatusAuth? _sentAuth;

    public TokenStatusServiceTests()
    {
        _target = new TokenStatusTargetRow
        {
            AttemptId = Guid.NewGuid(), EdcsApplicationRefNo = EdcsRef, CitizenId = _citizenId, DepartmentApplicationRefNo = "GSC-77",
            StatusUrl = "https://dept.example.gov.in/status", ApplicationId = _applicationId, ApplicationStatus = (int)ApplicationStatus.Submitted,
            ExtraConfig = TokenStatusJson.Write(new TokenStatusConfig("bearer", null, null, "data.state", "data.note",
                new Dictionary<string, string> { ["Pending"] = "UnderReview", ["Granted"] = "Approved" })),
            StatusSecretCiphertext = "cipher", DepartmentCode = "education",
        };
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _current.SetupGet(c => c.CitizenId).Returns(_citizenId);
        _integrations.Setup(i => i.GetStatusTargetAsync(EdcsRef, It.IsAny<CancellationToken>())).ReturnsAsync(_target);
        _secrets.Setup(s => s.TryUnprotect("cipher")).Returns("the-secret");
        Reply(200, """{"data":{"state":"Pending","note":"With the officer"}}""");
    }

    private void Reply(int status, string? body, string? error = null) =>
        _http.Setup(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TokenStatusAuth, CancellationToken>((_, b, a, _) => { _sentBody = b; _sentAuth = a; })
            .ReturnsAsync(new TokenStatusHttpResult(error is null ? status : null, body, error));

    private TokenStatusService Service() => new(
        _integrations.Object, _discovery.Object, _orchestration.Object, _status.Object, _http.Object, _secrets.Object,
        Mock.Of<SevaOne.Business.Discovery.Interfaces.IKaveriTokenService>(),
        new TokenApplicationWriter(_orchestration.Object, Mock.Of<SevaOne.Infrastructure.Repositories.Portal.IPortalRepository>(),
            Mock.Of<SevaOne.Infrastructure.Repositories.Notification.INotificationRepository>(), Mock.Of<SevaOne.Infrastructure.Repositories.Audit.IAuditRepository>(), _clock.Object),
        _current.Object, _clock.Object, NullLogger<TokenStatusService>.Instance);

    [Fact]
    public async Task A_mapped_word_moves_the_application_and_adds_a_timeline_event_and_an_outbox_message()
    {
        var result = await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        result.Should().Be(new TokenStatusRefreshDto(true, "Pending", "With the officer", false));
        _sentBody.Should().Be("""{"GscNumber":"GSC-77"}""");
        _sentAuth.Should().Be(new TokenStatusAuth("bearer", null, null, "the-secret"));
        _orchestration.Verify(o => o.UpdateApplicationStatusAsync(_applicationId, ApplicationStatus.UnderReview, It.IsAny<CancellationToken>()), Times.Once);
        _status.Verify(s => s.InsertApplicationEventAsync(
            It.Is<ApplicationEvent>(e => e.ApplicationId == _applicationId && e.Status == ApplicationStatus.UnderReview && e.Note.Contains("Pending") && e.Note.Contains("With the officer")),
            It.IsAny<CancellationToken>()), Times.Once);
        _orchestration.Verify(o => o.InsertOutboxMessageAsync(It.Is<OutboxMessage>(m => m.Topic == "application.status_changed"), It.IsAny<SevaOne.Infrastructure.Repositories.Common.IUnitOfWork?>(), It.IsAny<CancellationToken>()), Times.Once);
        _integrations.Verify(i => i.RecordStatusCheckAsync(_target.AttemptId, "Pending", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Asking_again_within_the_gap_does_not_call_the_department()
    {
        _target.StatusCheckedAt = Now.AddMinutes(-2);
        _target.DepartmentStatusText = "Pending";

        var result = await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        result.Should().Be(new TokenStatusRefreshDto(false, "Pending", null, false));
        _http.Verify(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task After_the_gap_it_asks_again()
    {
        _target.StatusCheckedAt = Now.AddMinutes(-6);

        (await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None))!.Checked.Should().BeTrue();
    }

    [Fact]
    public async Task Someone_elses_application_is_not_found()
    {
        _current.SetupGet(c => c.CitizenId).Returns(Guid.NewGuid());

        (await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None)).Should().BeNull();
        _http.Verify(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_word_we_have_no_mapping_for_is_shown_on_the_timeline_but_does_not_move_the_application()
    {
        Reply(200, """{"data":{"state":"Awaiting site visit"}}""");

        await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        _orchestration.Verify(o => o.UpdateApplicationStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStatus>(), It.IsAny<CancellationToken>()), Times.Never);
        _status.Verify(s => s.InsertApplicationEventAsync(
            It.Is<ApplicationEvent>(e => e.Status == ApplicationStatus.Submitted && e.Note.Contains("Awaiting site visit")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task The_same_unmapped_word_again_adds_nothing()
    {
        Reply(200, """{"data":{"state":"Awaiting site visit"}}""");
        _target.DepartmentStatusText = "Awaiting site visit";

        await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        _status.Verify(s => s.InsertApplicationEventAsync(It.IsAny<ApplicationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_status_the_application_already_has_adds_nothing()
    {
        _target.ApplicationStatus = (int)ApplicationStatus.UnderReview;

        await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        _orchestration.Verify(o => o.UpdateApplicationStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(500, "{}")]
    [InlineData(200, "not json")]
    [InlineData(200, """{"other":"x"}""")]
    public async Task A_failed_check_changes_nothing_and_is_recorded(int status, string body)
    {
        Reply(status, body);

        var result = await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        result!.Failed.Should().BeTrue();
        _orchestration.Verify(o => o.UpdateApplicationStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStatus>(), It.IsAny<CancellationToken>()), Times.Never);
        _status.Verify(s => s.InsertApplicationEventAsync(It.IsAny<ApplicationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _integrations.Verify(i => i.RecordStatusCheckAsync(_target.AttemptId, null, It.Is<string>(e => e.Length > 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_network_failure_is_a_failed_check()
    {
        Reply(0, null, error: "Could not reach the department's status API.");

        (await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None))!.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task A_secret_that_cannot_be_read_stops_before_calling_the_department()
    {
        _secrets.Setup(s => s.TryUnprotect("cipher")).Returns((string?)null);

        (await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None))!.Failed.Should().BeTrue();
        _http.Verify(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_background_check_asks_every_due_application_and_carries_on_after_a_failure()
    {
        _integrations.Setup(i => i.ListStatusDueAsync(It.IsAny<TimeSpan>(), 20, It.IsAny<CancellationToken>())).ReturnsAsync(["EDCS-BAD", EdcsRef]);
        _integrations.Setup(i => i.GetStatusTargetAsync("EDCS-BAD", It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        _target.StatusCheckedAt = Now.AddMinutes(-1); // the gap does not apply to the background check

        var asked = await Service().PollDueAsync(20, CancellationToken.None);

        asked.Should().Be(1);
        _orchestration.Verify(o => o.UpdateApplicationStatusAsync(_applicationId, ApplicationStatus.UnderReview, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task The_admin_test_shows_what_came_back_and_changes_no_application()
    {
        var scheme = new Scheme { Id = Guid.NewGuid() };
        _discovery.Setup(d => d.GetDepartmentByCodeAsync("education", It.IsAny<CancellationToken>())).ReturnsAsync(new Department { Id = Guid.NewGuid(), Code = "education" });
        _discovery.Setup(d => d.GetSchemeByCodeAsync(It.IsAny<Guid>(), "2971", It.IsAny<CancellationToken>())).ReturnsAsync(scheme);
        _integrations.Setup(i => i.GetAsync(scheme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new TokenIntegrationRow
        {
            TypeKey = "seva_sindhu_v3", StatusUrl = "https://dept.example.gov.in/status", HasStatusSecret = true, ExtraConfig = _target.ExtraConfig,
        });
        _integrations.Setup(i => i.GetStatusSecretCiphertextAsync(scheme.Id, It.IsAny<CancellationToken>())).ReturnsAsync("cipher");
        Reply(200, """{"data":{"state":"Granted"}}""");

        var result = await Service().TestAsync("education", "2971", "GSC-1", CancellationToken.None);

        result.DepartmentStatus.Should().Be("Granted");
        result.MappedStatus.Should().Be("Approved");
        result.Error.Should().BeNull();
        _sentBody.Should().Be("""{"GscNumber":"GSC-1"}""");
        _orchestration.Verify(o => o.UpdateApplicationStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_admin_test_needs_a_saved_status_url()
    {
        var scheme = new Scheme { Id = Guid.NewGuid() };
        _discovery.Setup(d => d.GetDepartmentByCodeAsync("education", It.IsAny<CancellationToken>())).ReturnsAsync(new Department { Id = Guid.NewGuid(), Code = "education" });
        _discovery.Setup(d => d.GetSchemeByCodeAsync(It.IsAny<Guid>(), "2971", It.IsAny<CancellationToken>())).ReturnsAsync(scheme);
        _integrations.Setup(i => i.GetAsync(scheme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new TokenIntegrationRow { TypeKey = "seva_sindhu_v3", StatusUrl = null });

        var act = () => Service().TestAsync("education", "2971", "GSC-1", CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
