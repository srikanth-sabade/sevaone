using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SevaOne.Business.Discovery.Services;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Security;

namespace SevaOne.UnitTests.Portal;

/// <summary>The original method, kept alongside "edcs_api" (Seva Sindhu's own redirect): Seva Sindhu's EP0115 API
/// issues the token and reference, and we post it to the department's own URL ourselves.</summary>
public class EdcsApiFormTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 9, 0, 0, TimeSpan.Zero);
    private const string Api = "https://sevasindhu.example/EP0115/api/TokenIntegration";
    private const string Landing = "https://dept.example.gov.in/SevaSindhuEntry.ashx";

    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<ITokenStatusHttpClient> _http = new();
    private readonly Mock<IAuditRepository> _audit = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Scheme _scheme = new() { Id = Guid.NewGuid(), ServiceCode = "2235", Name = "Ration card", SevasindhuServiceId = "2952" };
    private readonly Department _department = new() { Id = Guid.NewGuid(), Code = "food", Name = "Food" };
    private readonly Citizen _citizen = new() { Id = Guid.NewGuid(), SevaOneUserId = 4242, Mobile = "9000000099", DigiLockerId = "DL-1" };
    private readonly TokenIntegrationActiveRow _integration = new() { TypeKey = "edcs_api_form", LandingUrl = Landing };
    private TokenIntegrationAttemptWrite? _attempt;
    private string? _sentJson;

    public EdcsApiFormTokenTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _integrations.Setup(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()))
            .Callback<TokenIntegrationAttemptWrite, CancellationToken>((a, _) => _attempt = a).ReturnsAsync("EDCS295200000012");
    }

    private EdcsApiFormTokenService Service(string? baseUrl = Api) =>
        new(_integrations.Object, _http.Object, _audit.Object, _clock.Object, Options.Create(new EdcsApiOptions { BaseUrl = baseUrl }), NullLogger<EdcsApiFormTokenService>.Instance);

    private void ApiReplies(int status, string body) =>
        _http.Setup(h => h.PostJsonAsync($"{Api}/generate", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TokenStatusAuth, CancellationToken>((_, json, _, _) => _sentJson = json)
            .ReturnsAsync(new TokenStatusHttpResult(status, body, null));

    private const string Good = """{"AccessToken":"jwt.from.sevasindhu","TokenType":"Bearer","ExpiresIn":7199,"EdcsApplicationRefNo":"EDCS295200000012"}""";

    [Fact]
    public async Task Builds_and_posts_the_form_to_the_department_url_itself()
    {
        ApiReplies(200, Good);

        var outcome = await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        var sent = JsonDocument.Parse(_sentJson!).RootElement;
        sent.GetProperty("ServiceId").GetInt32().Should().Be(2952);

        outcome.ApplyUrl.Should().BeNull();
        outcome.PostForm!.Url.Should().Be(Landing);
        outcome.PostForm.Fields.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["UserId"] = "4242", ["ServiceId"] = "2952", ["edcsApplicationRefNo"] = "EDCS295200000012", ["assistedmode"] = "false", ["Token"] = "jwt.from.sevasindhu",
            ["mobile"] = _citizen.Mobile!, ["digilockerid"] = _citizen.DigiLockerId!,
        });
        _attempt!.LandingUrl.Should().Be(Landing);
    }

    [Fact]
    public async Task Refuses_when_the_integration_has_no_department_url()
    {
        ApiReplies(200, Good);
        _integration.LandingUrl = null;

        var act = () => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _integrations.Verify(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sends_key1_through_key5_only_when_the_service_has_them_set()
    {
        ApiReplies(200, Good);
        _integration.ExtraConfig = """{"edcsKeys":{"key2":"some-value"}}""";

        await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        var sent = JsonDocument.Parse(_sentJson!).RootElement;
        sent.GetProperty("Key2").GetString().Should().Be("some-value");
        sent.TryGetProperty("Key1", out _).Should().BeFalse();
    }
}
