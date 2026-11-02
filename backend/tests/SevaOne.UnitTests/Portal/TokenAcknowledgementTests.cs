using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Moq;
using SevaOne.Business.Common;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Services;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Common;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Notification;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;

namespace SevaOne.UnitTests.Portal;

/// <summary>The department-facing acknowledgement API of the token integration (Seva Sindhu V3, URL 2).</summary>
public class TokenAcknowledgementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private const string EdcsRef = "EDCS100100000001";

    private readonly Guid _departmentId = Guid.NewGuid();
    private readonly Guid _citizenId = Guid.NewGuid();
    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<IOrchestrationRepository> _orchestration = new();
    private readonly Mock<IStatusRepository> _status = new();
    private readonly Mock<IPortalRepository> _portal = new();
    private readonly Mock<INotificationRepository> _notification = new();
    private readonly Mock<IAuditRepository> _audit = new();
    private readonly Mock<IApplyRedirectProtector> _protector = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ICurrentCitizen> _current = new();

    private readonly TokenIntegrationAttemptRow _attempt;
    private readonly TokenIntegrationCaller _caller;
    private TokenIntegrationAckWrite? _written;
    private ServiceApplication? _createdApplication;
    private ApplicationEvent? _firstEvent;

    public TokenAcknowledgementTests()
    {
        _attempt = new TokenIntegrationAttemptRow
        {
            AttemptId = Guid.NewGuid(), EdcsApplicationRefNo = EdcsRef, ServiceId = Guid.NewGuid(), DepartmentId = _departmentId,
            DepartmentCode = "education", CitizenId = _citizenId, SevaOneUserId = 4242, TokenHash = Sha("jws-plain"),
            ServiceName = "Renewal of recognition", SakalaDays = 15, AllowedIps = [],
        };
        _caller = new TokenIntegrationCaller(Guid.NewGuid(), _departmentId, "education");

        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _integrations.Setup(i => i.GetAttemptAsync(EdcsRef, It.IsAny<CancellationToken>())).ReturnsAsync(_attempt);
        _integrations.Setup(i => i.ApplyAcknowledgementAsync(It.IsAny<TokenIntegrationAckWrite>(), It.IsAny<CancellationToken>()))
            .Callback<TokenIntegrationAckWrite, CancellationToken>((w, _) => _written = w)
            .ReturnsAsync(new TokenIntegrationAckResult { UpdateId = 7, AlreadyReceived = false });
        _portal.Setup(p => p.GetCitizenByIdAsync(_citizenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Citizen { Id = _citizenId, Name = "Asha Rao", Address = "12 MG Road", District = "Mysuru", Mobile = "9000000099" });
        _orchestration.Setup(o => o.GetApplicationByReferenceAsync(EdcsRef, It.IsAny<CancellationToken>())).ReturnsAsync((ServiceApplication?)null);
        _orchestration.Setup(o => o.SubmitApplicationAsync(It.IsAny<ServiceApplication>(), It.IsAny<ApplicationEvent>(), It.IsAny<IUnitOfWork?>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceApplication, ApplicationEvent, IUnitOfWork?, CancellationToken>((a, e, _, _) => { _createdApplication = a; _firstEvent = e; })
            .Returns(Task.CompletedTask);
        _protector.Setup(p => p.TryUnprotect("jws-encrypted")).Returns("jws-plain");
    }

    private TokenAcknowledgementService Service() => new(
        _integrations.Object, _orchestration.Object, _status.Object,
        new TokenApplicationWriter(_orchestration.Object, _portal.Object, _notification.Object, _audit.Object, _clock.Object),
        _protector.Object, _clock.Object, _current.Object);

    private static string Sha(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static TokenAckRequest Request(Action<TokenAckRequest>? change = null)
    {
        var request = new TokenAckRequest
        {
            UserId = "4242", DepartmentApplicationRefNo = "RD01112342342", EdcsApplicationRefNo = EdcsRef, Token = "jws-encrypted",
            DepartmentPaymentStatus = "Paid", DepartmentFeeCollectionDate = "2024-08-17T10:30:00", DepartmentFee = 10m,
            DepartmentPaymentRefNumber = "PAYREF1001", AcknowledgmentFileType = "pdf", AcknowledgmentDate = "2024-08-17T10:31:00",
            Acknowledgment = Convert.ToBase64String("%PDF-1.4 test"u8.ToArray()),
        };
        change?.Invoke(request);
        return request;
    }

    [Fact]
    public async Task A_valid_acknowledgement_is_recorded_and_the_application_appears_on_the_dashboard()
    {
        var outcome = await Service().AcknowledgeAsync(Request(), _caller, "103.21.4.10", CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Success.Should().Be(new TokenAckSuccess(7, EdcsRef, "RD01112342342"));
        _written!.PaymentStatus.Should().Be("Paid");
        _written.Fee.Should().Be(10m);
        _written.FileType.Should().Be("pdf");
        _written.TokenVerified.Should().BeTrue();
        _written.Payload.Should().NotContain("jws").And.NotContain("%PDF");

        _createdApplication.Should().NotBeNull();
        _createdApplication!.SevaReference.Should().Be(EdcsRef);
        _createdApplication.DepartmentReference.Should().Be("RD01112342342");
        _createdApplication.CitizenId.Should().Be(_citizenId);
        _createdApplication.Status.Should().Be(ApplicationStatus.Submitted);
        _createdApplication.ApplicantName.Should().Be("Asha Rao");
        _createdApplication.SakalaDueAt.Should().Be(_createdApplication.SubmittedAt.AddDays(15));
        _firstEvent!.Note.Should().Contain("RD01112342342").And.Contain("Paid");
    }

    [Fact]
    public async Task Dates_without_a_zone_are_read_as_Indian_Standard_Time()
    {
        await Service().AcknowledgeAsync(Request(), _caller, null, CancellationToken.None);

        _written!.FeeCollectionDate.Should().Be(new DateTimeOffset(2024, 8, 17, 10, 30, 0, TimeSpan.FromHours(5.5)));
    }

    [Fact]
    public async Task A_repeated_identical_call_returns_the_same_id_and_adds_no_second_event()
    {
        _integrations.Setup(i => i.ApplyAcknowledgementAsync(It.IsAny<TokenIntegrationAckWrite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenIntegrationAckResult { UpdateId = 7, AlreadyReceived = true });
        _orchestration.Setup(o => o.GetApplicationByReferenceAsync(EdcsRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceApplication { Id = Guid.NewGuid(), SevaReference = EdcsRef, Status = ApplicationStatus.Submitted });

        var outcome = await Service().AcknowledgeAsync(Request(), _caller, null, CancellationToken.None);

        outcome.Success!.DepartmentApplicationUpdateId.Should().Be(7);
        _status.Verify(s => s.InsertApplicationEventAsync(It.IsAny<ApplicationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _orchestration.Verify(o => o.SubmitApplicationAsync(It.IsAny<ServiceApplication>(), It.IsAny<ApplicationEvent>(), It.IsAny<IUnitOfWork?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_changed_acknowledgement_for_an_existing_application_adds_a_status_event()
    {
        var existing = new ServiceApplication { Id = Guid.NewGuid(), SevaReference = EdcsRef, Status = ApplicationStatus.UnderReview };
        _orchestration.Setup(o => o.GetApplicationByReferenceAsync(EdcsRef, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await Service().AcknowledgeAsync(Request(r => r.DepartmentPaymentStatus = "Refunded"), _caller, null, CancellationToken.None);

        _status.Verify(s => s.InsertApplicationEventAsync(
            It.Is<ApplicationEvent>(e => e.ApplicationId == existing.Id && e.Status == ApplicationStatus.UnderReview && e.Note.Contains("Refunded")),
            It.IsAny<CancellationToken>()), Times.Once);
        _orchestration.Verify(o => o.SubmitApplicationAsync(It.IsAny<ServiceApplication>(), It.IsAny<ApplicationEvent>(), It.IsAny<IUnitOfWork?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task An_unknown_reference_is_refused()
    {
        var outcome = await Service().AcknowledgeAsync(Request(r => r.EdcsApplicationRefNo = "EDCS999"), _caller, null, CancellationToken.None);

        outcome.IsSuccess.Should().BeFalse();
        _integrations.Verify(i => i.ApplyAcknowledgementAsync(It.IsAny<TokenIntegrationAckWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_department_cannot_acknowledge_another_departments_application()
    {
        var other = _caller with { DepartmentId = Guid.NewGuid(), DepartmentCode = "revenue" };

        var outcome = await Service().AcknowledgeAsync(Request(), other, null, CancellationToken.None);

        outcome.IsSuccess.Should().BeFalse();
        outcome.Reason.Should().Contain("different department");
        _integrations.Verify(i => i.ApplyAcknowledgementAsync(It.IsAny<TokenIntegrationAckWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("103.21.4.10", true)]
    [InlineData("10.20.0.77", true)]
    [InlineData("10.21.0.1", false)]
    [InlineData(null, false)]
    public async Task The_service_IP_list_is_enforced_when_there_is_one(string? ip, bool allowed)
    {
        _attempt.AllowedIps = ["103.21.4.10", "10.20.0.0/24"];

        var outcome = await Service().AcknowledgeAsync(Request(), _caller, ip, CancellationToken.None);

        outcome.IsSuccess.Should().Be(allowed);
    }

    [Fact]
    public async Task A_token_that_is_not_the_one_we_issued_is_refused()
    {
        _protector.Setup(p => p.TryUnprotect("forged")).Returns("something-else");

        var outcome = await Service().AcknowledgeAsync(Request(r => r.Token = "forged"), _caller, null, CancellationToken.None);

        outcome.IsSuccess.Should().BeFalse();
        outcome.Reason.Should().Contain("Token");
    }

    [Fact]
    public async Task A_missing_token_is_accepted_but_not_marked_verified()
    {
        var outcome = await Service().AcknowledgeAsync(Request(r => r.Token = null), _caller, null, CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        _written!.TokenVerified.Should().BeNull();
    }

    [Fact]
    public async Task A_user_id_that_does_not_match_is_refused()
    {
        var outcome = await Service().AcknowledgeAsync(Request(r => r.UserId = "9999"), _caller, null, CancellationToken.None);

        outcome.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("pdf", "not really a pdf")]
    [InlineData("exe", "%PDF-1.4 test")]
    public async Task A_file_that_is_not_what_it_says_it_is_is_refused(string type, string content)
    {
        var outcome = await Service().AcknowledgeAsync(
            Request(r => { r.AcknowledgmentFileType = type; r.Acknowledgment = Convert.ToBase64String(Encoding.ASCII.GetBytes(content)); }),
            _caller, null, CancellationToken.None);

        outcome.IsSuccess.Should().BeFalse();
        _integrations.Verify(i => i.ApplyAcknowledgementAsync(It.IsAny<TokenIntegrationAckWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Bad_base64_a_negative_fee_and_a_bad_date_are_refused()
    {
        (await Service().AcknowledgeAsync(Request(r => r.Acknowledgment = "!!!not base64!!!"), _caller, null, CancellationToken.None)).IsSuccess.Should().BeFalse();
        (await Service().AcknowledgeAsync(Request(r => r.DepartmentFee = -1m), _caller, null, CancellationToken.None)).IsSuccess.Should().BeFalse();
        (await Service().AcknowledgeAsync(Request(r => r.AcknowledgmentDate = "yesterday-ish"), _caller, null, CancellationToken.None)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Login_works_with_the_right_password_and_not_otherwise()
    {
        var hash = TokenIntegrationSecrets.Hash("s3cret-Passw0rd");
        _integrations.Setup(i => i.GetCredentialAsync("tok-education-a1b2c3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenIntegrationCredentialRow { Id = Guid.NewGuid(), DepartmentId = _departmentId, DepartmentCode = "education", PasswordHash = hash, IsActive = true });
        string Basic(string user, string pass) => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));

        (await Service().AuthenticateAsync(Basic("tok-education-a1b2c3", "s3cret-Passw0rd"), CancellationToken.None))!.DepartmentCode.Should().Be("education");
        (await Service().AuthenticateAsync(Basic("tok-education-a1b2c3", "wrong"), CancellationToken.None)).Should().BeNull();
        (await Service().AuthenticateAsync(Basic("nobody", "s3cret-Passw0rd"), CancellationToken.None)).Should().BeNull();
        (await Service().AuthenticateAsync(null, CancellationToken.None)).Should().BeNull();
        (await Service().AuthenticateAsync("Bearer abc", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task A_disabled_login_cannot_be_used()
    {
        _integrations.Setup(i => i.GetCredentialAsync("tok-off", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenIntegrationCredentialRow { Id = Guid.NewGuid(), DepartmentId = _departmentId, PasswordHash = TokenIntegrationSecrets.Hash("pw"), IsActive = false });

        var caller = await Service().AuthenticateAsync("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("tok-off:pw")), CancellationToken.None);

        caller.Should().BeNull();
    }

    [Fact]
    public async Task A_sandbox_acknowledgement_is_recorded_but_never_creates_an_application()
    {
        _attempt.IsSandbox = true;

        var outcome = await Service().AcknowledgeAsync(Request(), _caller, null, CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        _written.Should().NotBeNull();
        _createdApplication.Should().BeNull();
        _orchestration.Verify(o => o.SubmitApplicationAsync(It.IsAny<ServiceApplication>(), It.IsAny<ApplicationEvent>(), It.IsAny<IUnitOfWork?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_service_IP_list_does_not_apply_to_a_sandbox_acknowledgement()
    {
        _attempt.IsSandbox = true;
        _attempt.AllowedIps = ["103.21.4.10"];

        (await Service().AcknowledgeAsync(Request(), _caller, "8.8.8.8", CancellationToken.None)).IsSuccess.Should().BeTrue();
    }
}
