using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Business.Discovery.Services;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Notification;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;

namespace SevaOne.UnitTests.Portal;

internal sealed class MemoryTokenCache : ITokenCache
{
    public readonly Dictionary<string, string> Values = new();
    public bool LockBusy;

    public Task<string?> GetAsync(string key, CancellationToken ct) => Task.FromResult(Values.TryGetValue(key, out var v) ? v : null);
    public Task SetAsync(string key, string value, TimeSpan lifetime, CancellationToken ct) { Values[key] = value; return Task.CompletedTask; }
    public Task RemoveAsync(string key, CancellationToken ct) { Values.Remove(key); return Task.CompletedTask; }
    public Task<IAsyncDisposable?> TryLockAsync(string key, TimeSpan lifetime, CancellationToken ct) =>
        Task.FromResult<IAsyncDisposable?>(LockBusy ? null : new NoopLock());
    private sealed class NoopLock : IAsyncDisposable { public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
}

/// <summary>Kaveri 2.0 sign-in: one technical login, a 300-second access token and a single-use refresh token.</summary>
public class KaveriTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly MemoryTokenCache _cache = new();
    private readonly Mock<ITokenStatusHttpClient> _http = new();
    private readonly Mock<ITokenStatusSecretProtector> _secrets = new();
    private readonly Mock<IClock> _clock = new();
    private readonly List<(string Url, string Body)> _calls = [];

    public KaveriTokenServiceTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _secrets.Setup(s => s.Protect(It.IsAny<string>())).Returns<string>(x => "enc:" + x);
        _secrets.Setup(s => s.TryUnprotect(It.IsAny<string>())).Returns<string>(x => x.StartsWith("enc:") ? x[4..] : null);
        _integrations.Setup(i => i.GetProviderAsync("kaveri_sso", It.IsAny<CancellationToken>())).ReturnsAsync(new TokenProviderRow
        {
            TypeKey = "kaveri_sso", SecretCiphertext = "enc:the-password",
            SettingsJson = """{"loginUrl":"https://k.example.gov.in/login","refreshUrl":"https://k.example.gov.in/refresh","ssoUrl":"https://k.example.gov.in/sso","statusUrl":"https://k.example.gov.in/status","username":"svc-user"}""",
        });
        Login("access-1", "refresh-1");
        Refresh("access-2", "refresh-2");
    }

    private void Login(string access, string refresh) =>
        _http.Setup(h => h.PostJsonAsync("https://k.example.gov.in/login", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TokenStatusAuth, CancellationToken>((u, b, _, _) => _calls.Add((u, b)))
            .ReturnsAsync(new TokenStatusHttpResult(200, $$$"""{"success":true,"data":{"Citizenid":1,"JwtToken":"{{{access}}}","RefreshToken":"{{{refresh}}}"}}""", null));

    private void Refresh(string access, string refresh) =>
        _http.Setup(h => h.PostJsonAsync("https://k.example.gov.in/refresh", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TokenStatusAuth, CancellationToken>((u, b, _, _) => _calls.Add((u, b)))
            .ReturnsAsync(new TokenStatusHttpResult(200, $$"""{"access_token":"{{access}}","expires_in":300,"refresh_token":"{{refresh}}","refresh_expires_in":1800,"token_type":"bearer"}""", null));

    private KaveriTokenService Service() => new(_integrations.Object, _cache, _http.Object, _secrets.Object, _clock.Object);

    [Fact]
    public async Task The_first_call_signs_in_and_the_next_uses_the_stored_token()
    {
        (await Service().GetAccessTokenAsync(false, CancellationToken.None)).AccessToken.Should().Be("access-1");
        (await Service().GetAccessTokenAsync(false, CancellationToken.None)).AccessToken.Should().Be("access-1");

        _calls.Should().ContainSingle(c => c.Url.EndsWith("/login"));
        JsonDocument.Parse(_calls[0].Body).RootElement.GetProperty("username").GetString().Should().Be("svc-user");
        JsonDocument.Parse(_calls[0].Body).RootElement.GetProperty("password").GetString().Should().Be("the-password");
    }

    [Fact]
    public async Task Tokens_are_stored_encrypted()
    {
        await Service().GetAccessTokenAsync(false, CancellationToken.None);

        _cache.Values.Values.Should().OnlyContain(v => v.StartsWith("enc:"));
    }

    [Fact]
    public async Task When_the_access_token_has_gone_the_refresh_token_is_used_once_and_replaced()
    {
        await Service().GetAccessTokenAsync(false, CancellationToken.None);
        _cache.Values.Remove("kaveri:access"); // five minutes passed

        var result = await Service().GetAccessTokenAsync(false, CancellationToken.None);

        result.AccessToken.Should().Be("access-2");
        _calls.Should().Contain(c => c.Url.EndsWith("/refresh") && c.Body.Contains("refresh-1"));
        _cache.Values["kaveri:refresh"].Should().Be("enc:refresh-2"); // the spent one is gone
    }

    [Fact]
    public async Task A_refused_refresh_falls_back_to_a_fresh_sign_in()
    {
        await Service().GetAccessTokenAsync(false, CancellationToken.None);
        _cache.Values.Remove("kaveri:access");
        _http.Setup(h => h.PostJsonAsync("https://k.example.gov.in/refresh", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenStatusHttpResult(401, """{"success":false,"errorCode":"UNAUTHORIZED"}""", null));
        Login("access-3", "refresh-3");

        (await Service().GetAccessTokenAsync(false, CancellationToken.None)).AccessToken.Should().Be("access-3");
    }

    [Fact]
    public async Task A_token_Kaveri_rejected_is_renewed_when_asked()
    {
        await Service().GetAccessTokenAsync(false, CancellationToken.None);

        (await Service().GetAccessTokenAsync(forceNew: true, CancellationToken.None)).AccessToken.Should().Be("access-2");
    }

    [Fact]
    public async Task A_refused_sign_in_says_so_and_stores_nothing()
    {
        _http.Setup(h => h.PostJsonAsync("https://k.example.gov.in/login", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenStatusHttpResult(401, """{"success":false,"errorCode":"UNAUTHORIZED","errorMessage":"Invalid credentials."}""", null));

        var result = await Service().GetAccessTokenAsync(false, CancellationToken.None);

        result.AccessToken.Should().BeNull();
        result.Error.Should().Contain("401").And.Contain("Invalid credentials");
        _cache.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task Incomplete_settings_stop_before_any_call()
    {
        _integrations.Setup(i => i.GetProviderAsync("kaveri_sso", It.IsAny<CancellationToken>())).ReturnsAsync((TokenProviderRow?)null);

        var result = await Service().GetAccessTokenAsync(false, CancellationToken.None);

        result.AccessToken.Should().BeNull();
        _http.Verify(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_caller_that_cannot_take_the_lock_waits_for_the_other_to_finish()
    {
        _cache.LockBusy = true;
        var waiting = Service().GetAccessTokenAsync(false, CancellationToken.None);
        await Task.Delay(700);
        _cache.Values["kaveri:access"] = "enc:shared-token";

        (await waiting).AccessToken.Should().Be("shared-token");
        _http.Verify(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_admin_test_signs_in_for_real_and_never_returns_the_token()
    {
        await Service().GetAccessTokenAsync(false, CancellationToken.None); // a token is already stored

        var result = await Service().TestLoginAsync(CancellationToken.None);

        result.Ok.Should().BeTrue();
        result.Message.Should().NotContain("access");
        _calls.Count(c => c.Url.EndsWith("/login")).Should().Be(2);
    }
}

public class KaveriSsoServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<IKaveriTokenService> _tokens = new();
    private readonly Mock<ITokenStatusHttpClient> _http = new();
    private readonly Mock<IAuditRepository> _audit = new();
    private readonly Mock<IClock> _clock = new();
    private readonly KaveriSettings _settings = new("https://k.example.gov.in/login", "https://k.example.gov.in/refresh", "https://staging.kaveri.example.gov.in/api/sso", "https://k.example.gov.in/status", "svc-user");
    private readonly Citizen _citizen = new()
    {
        Id = Guid.NewGuid(), SevaOneUserId = 4242, Name = "Chandan S", Email = "chandan@example.com", Mobile = "+91 96116-46906", Gender = "Male",
        Address = "Tumkur Main Road", District = "Bengaluru Urban", StateName = "Karnataka", DigiLockerId = "DL-1",
    };
    private readonly Scheme _scheme = new() { Id = Guid.NewGuid(), ServiceCode = "KAV-1", Name = "Registration of property" };
    private readonly Department _department = new() { Id = Guid.NewGuid(), Code = "revenue", Name = "Revenue Department" };
    private readonly TokenIntegrationActiveRow _integration = new() { TypeKey = "kaveri_sso", ExtraConfig = TokenStatusJson.Write(new TokenStatusConfig(), 12345) };
    private TokenIntegrationAttemptWrite? _attempt;
    private string? _sentBody;
    private TokenStatusAuth? _sentAuth;
    private readonly Guid _attemptId = Guid.NewGuid();

    public KaveriSsoServiceTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _tokens.Setup(t => t.LoadProviderAsync(It.IsAny<CancellationToken>())).ReturnsAsync((_settings, "pw", (string?)null));
        _tokens.Setup(t => t.GetAccessTokenAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(new KaveriTokenResult("access-1", null));
        _integrations.Setup(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()))
            .Callback<TokenIntegrationAttemptWrite, CancellationToken>((a, _) => _attempt = a).ReturnsAsync("EDCS100100000009");
        _integrations.Setup(i => i.GetAttemptAsync("EDCS100100000009", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenIntegrationAttemptRow { AttemptId = _attemptId, EdcsApplicationRefNo = "EDCS100100000009" });
        Reply(200, """{"success":true,"data":{"redirectUrl":"https://kaveri.example.gov.in/authenticate?id=SECRET-SESSION"},"correlationId":"x"}""");
    }

    private void Reply(int status, string body) =>
        _http.Setup(h => h.PostJsonAsync(_settings.SsoUrl, It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TokenStatusAuth, CancellationToken>((_, b, a, _) => { _sentBody = b; _sentAuth = a; })
            .ReturnsAsync(new TokenStatusHttpResult(status, body, null));

    private KaveriSsoService Service() => new(_integrations.Object, _tokens.Object, _http.Object, _audit.Object, _clock.Object, NullLogger<KaveriSsoService>.Instance);

    [Fact]
    public async Task The_citizen_is_sent_to_the_address_Kaveri_returns()
    {
        var url = await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        url.Should().Be("https://kaveri.example.gov.in/authenticate?id=SECRET-SESSION");
        _sentAuth.Should().Be(new TokenStatusAuth("bearer", null, null, "access-1"));
        var body = JsonDocument.Parse(_sentBody!).RootElement;
        body.GetProperty("full_name").GetString().Should().Be("Chandan S");
        body.GetProperty("mobile").GetString().Should().Be("9611646906");
        body.GetProperty("gender").GetString().Should().Be("M");
        body.GetProperty("emailVerified").GetBoolean().Should().BeFalse();
        body.GetProperty("EdcsserviceId").GetInt64().Should().Be(12345);
        body.GetProperty("EdcsapplicationNumber").GetString().Should().Be("EDCS100100000009");
        body.GetProperty("digilockerid").GetString().Should().Be("DL-1");
        body.TryGetProperty("aadhaarHash", out _).Should().BeFalse(); // nothing to send, so left out
    }

    [Fact]
    public async Task Only_the_service_id_is_recorded_and_the_session_token_stays_out_of_the_record()
    {
        await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        _attempt!.SentFields.Should().Be("""{"edcsServiceId":12345}""");
        _attempt.SentFields.Should().NotContain("Chandan").And.NotContain("example.com").And.NotContain("96116");
        _integrations.Verify(i => i.FinishAttemptAsync(_attemptId, "https://kaveri.example.gov.in/authenticate", "initiated", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task The_verified_email_flag_follows_the_setting()
    {
        _tokens.Setup(t => t.LoadProviderAsync(It.IsAny<CancellationToken>())).ReturnsAsync((_settings with { AssumeEmailVerified = true }, "pw", (string?)null));

        await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        JsonDocument.Parse(_sentBody!).RootElement.GetProperty("emailVerified").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task A_rejected_token_is_renewed_and_the_call_repeated_once()
    {
        var calls = 0;
        _http.Setup(h => h.PostJsonAsync(_settings.SsoUrl, It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++calls == 1
                ? new TokenStatusHttpResult(401, """{"success":false,"errorCode":"UNAUTHORIZED"}""", null)
                : new TokenStatusHttpResult(200, """{"success":true,"data":{"redirectUrl":"https://kaveri.example.gov.in/authenticate?id=X"}}""", null));

        var url = await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        url.Should().StartWith("https://kaveri.example.gov.in/authenticate");
        _tokens.Verify(t => t.GetAccessTokenAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_missing_profile_detail_is_named_and_nothing_is_started()
    {
        _citizen.Email = null;
        _citizen.Gender = null;

        var act = () => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).WithMessage("*email address*gender*");
        _integrations.Verify(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()), Times.Never);
        _http.Verify(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Kaveri_refusing_the_details_gives_the_citizen_its_message()
    {
        Reply(400, """{"success":false,"errorCode":"INVALID_REQUEST","errorMessage":"Mobile number already registered."}""");

        var act = () => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).WithMessage("*Mobile number already registered*");
        _integrations.Verify(i => i.FinishAttemptAsync(_attemptId, null, "failed", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(503)]
    [InlineData(500)]
    [InlineData(403)]
    public async Task A_kaveri_outage_is_a_plain_not_available_message(int status)
    {
        Reply(status, """{"success":false,"errorCode":"TEMPORARY_UNAVAILABLE"}""");

        var act = () => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).WithMessage("*not available*");
    }

    [Theory]
    [InlineData("https://evil.example.com/authenticate?id=1")]
    [InlineData("http://kaveri.example.gov.in/authenticate?id=1")]
    [InlineData("https://user:pw@kaveri.example.gov.in/x")]
    [InlineData("javascript:alert(1)")]
    public async Task An_address_off_Kaveris_domain_or_not_https_is_refused(string redirect)
    {
        Reply(200, JsonSerializer.Serialize(new { success = true, data = new { redirectUrl = redirect } }));

        var act = () => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task A_service_without_a_kaveri_id_or_with_incomplete_settings_is_not_available()
    {
        _integration.ExtraConfig = TokenStatusJson.Write(new TokenStatusConfig());
        (await Assert.ThrowsAsync<ConflictException>(() => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None))).Message.Should().Contain("not available");

        _integration.ExtraConfig = TokenStatusJson.Write(new TokenStatusConfig(), 1);
        _tokens.Setup(t => t.LoadProviderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(((KaveriSettings?)null, (string?)null, "incomplete"));
        await Assert.ThrowsAsync<ConflictException>(() => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None));
    }

    [Theory]
    [InlineData("9611646906", "9611646906")]
    [InlineData("+91 96116 46906", "9611646906")]
    [InlineData("09611646906", "9611646906")]
    [InlineData("919611646906", "9611646906")]
    [InlineData("12345", null)]
    [InlineData("5611646906", null)]
    [InlineData(null, null)]
    public void Mobile_numbers_are_tidied_to_ten_digits(string? input, string? expected) => KaveriSsoService.NormalizeMobile(input).Should().Be(expected);

    [Theory]
    [InlineData("Male", "M")]
    [InlineData("female", "F")]
    [InlineData("Transgender", "T")]
    [InlineData("Other", "T")]
    [InlineData("", null)]
    [InlineData("unknown", null)]
    public void Gender_is_mapped_to_M_F_or_T(string input, string? expected) => KaveriSsoService.MapGender(input).Should().Be(expected);

    [Theory]
    [InlineData("https://kaveri.karnataka.gov.in/authenticate?id=1", "https://staging.kaveri.karnataka.gov.in/api/SevaOne_SSO_Request", true)]
    [InlineData("https://staging.kaveri.karnataka.gov.in/authenticate?id=1", "https://staging.kaveri.karnataka.gov.in/api/SevaOne_SSO_Request", true)]
    [InlineData("https://karnataka.gov.in/authenticate", "https://staging.kaveri.karnataka.gov.in/api/x", false)]
    [InlineData("https://mock-kaveri/authenticate", "http://mock-kaveri:8099/sso", true)]
    [InlineData("https://other-host/authenticate", "http://mock-kaveri:8099/sso", false)]
    public void The_redirect_must_be_on_Kaveris_own_domain(string redirect, string sso, bool allowed) => KaveriSsoService.RedirectAllowed(redirect, sso).Should().Be(allowed);
}

/// <summary>The status check, with Kaveri's own sign-in and its "ApplicationNumber" request.</summary>
public class KaveriStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private const string EdcsRef = "EDCS100100000009";
    private readonly Guid _citizenId = Guid.NewGuid();
    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<IOrchestrationRepository> _orchestration = new();
    private readonly Mock<ITokenStatusHttpClient> _http = new();
    private readonly Mock<IKaveriTokenService> _kaveri = new();
    private readonly Mock<ICurrentCitizenStub> _unused = new();
    private readonly Mock<SevaOne.Business.Common.ICurrentCitizen> _current = new();
    private readonly Mock<IClock> _clock = new();
    private readonly TokenStatusTargetRow _target;
    private readonly List<(string Body, TokenStatusAuth Auth)> _sent = [];

    public interface ICurrentCitizenStub { }

    public KaveriStatusTests()
    {
        _target = new TokenStatusTargetRow
        {
            AttemptId = Guid.NewGuid(), EdcsApplicationRefNo = EdcsRef, CitizenId = _citizenId, DepartmentApplicationRefNo = "",
            StatusUrl = "https://k.example.gov.in/status", ApplicationId = null, DepartmentCode = "revenue", TypeKey = "kaveri_sso",
            ServiceId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), ServiceName = "Registration of property", SakalaDays = 15,
            ExtraConfig = TokenStatusJson.Write(new TokenStatusConfig("provider", null, null, "data.Status", null,
                new Dictionary<string, string> { ["Application Submitted"] = "Submitted", ["Under Scrutiny"] = "UnderReview", ["Registered"] = "Approved" }, "ApplicationNumber", "edcs"), 77),
        };
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _current.SetupGet(c => c.CitizenId).Returns(_citizenId);
        _integrations.Setup(i => i.GetStatusTargetAsync(EdcsRef, It.IsAny<CancellationToken>())).ReturnsAsync(_target);
        _kaveri.Setup(k => k.GetAccessTokenAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(new KaveriTokenResult("access-1", null));
        Reply(200, """{"success":true,"data":{"ApplicationNumber":"PRP1","Status":"Under Scrutiny"}}""");
    }

    private void Reply(int status, string body) =>
        _http.Setup(h => h.PostJsonAsync("https://k.example.gov.in/status", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TokenStatusAuth, CancellationToken>((_, b, a, _) => _sent.Add((b, a)))
            .ReturnsAsync(new TokenStatusHttpResult(status, body, null));

    private TokenStatusService Service() => new(
        _integrations.Object, Mock.Of<IDiscoveryRepository>(), _orchestration.Object, Mock.Of<IStatusRepository>(), _http.Object,
        Mock.Of<ITokenStatusSecretProtector>(), _kaveri.Object,
        new TokenApplicationWriter(_orchestration.Object, Mock.Of<IPortalRepository>(), Mock.Of<INotificationRepository>(), Mock.Of<IAuditRepository>(), _clock.Object),
        _current.Object, _clock.Object, NullLogger<TokenStatusService>.Instance);

    [Fact]
    public async Task The_first_status_found_puts_the_application_on_the_dashboard()
    {
        ServiceApplication? created = null;
        _orchestration.Setup(o => o.SubmitApplicationAsync(It.IsAny<ServiceApplication>(), It.IsAny<ApplicationEvent>(), It.IsAny<SevaOne.Infrastructure.Repositories.Common.IUnitOfWork?>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceApplication, ApplicationEvent, SevaOne.Infrastructure.Repositories.Common.IUnitOfWork?, CancellationToken>((a, _, _, _) => created = a).Returns(Task.CompletedTask);

        var result = await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        result.Should().Be(new TokenStatusRefreshDto(true, "Under Scrutiny", null, false));
        created.Should().NotBeNull();
        created!.SevaReference.Should().Be(EdcsRef);
        created.DepartmentReference.Should().Be("");
        created.Status.Should().Be(ApplicationStatus.UnderReview);
        _sent.Single().Body.Should().Be($$"""{"ApplicationNumber":"{{EdcsRef}}"}"""); // the number source is ours
        _sent.Single().Auth.Should().Be(new TokenStatusAuth("bearer", null, null, "access-1"));
    }

    [Fact]
    public async Task A_not_found_answer_means_not_applied_yet_and_is_not_an_error()
    {
        Reply(404, """{"success":false,"errorCode":"NOT_FOUND","errorMessage":"Application not found."}""");

        var result = await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        result!.Failed.Should().BeFalse();
        _orchestration.Verify(o => o.SubmitApplicationAsync(It.IsAny<ServiceApplication>(), It.IsAny<ApplicationEvent>(), It.IsAny<SevaOne.Infrastructure.Repositories.Common.IUnitOfWork?>(), It.IsAny<CancellationToken>()), Times.Never);
        _integrations.Verify(i => i.RecordStatusCheckAsync(_target.AttemptId, null, It.Is<string>(e => e.Contains("Not found")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_refused_token_is_renewed_once_and_the_call_repeated()
    {
        var calls = 0;
        _http.Setup(h => h.PostJsonAsync("https://k.example.gov.in/status", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++calls == 1
                ? new TokenStatusHttpResult(401, """{"success":false}""", null)
                : new TokenStatusHttpResult(200, """{"success":true,"data":{"Status":"Registered"}}""", null));

        var result = await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None);

        result!.DepartmentStatus.Should().Be("Registered");
        _kaveri.Verify(k => k.GetAccessTokenAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Failing_to_sign_in_is_a_failed_check()
    {
        _kaveri.Setup(k => k.GetAccessTokenAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(new KaveriTokenResult(null, "Kaveri sign-in is busy."));

        (await Service().RefreshForCitizenAsync(EdcsRef, CancellationToken.None))!.Failed.Should().BeTrue();
    }
}
