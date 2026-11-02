using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SevaOne.Business.Discovery.Services;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Security;

namespace SevaOne.UnitTests.Portal;

/// <summary>Token integration where Seva Sindhu's EP0115 API issues the token and the application reference.</summary>
public class EdcsApiTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);
    private const string Api = "https://sevasindhu.example/EP0115/api/TokenIntegration";
    private const string Landing = "https://dept.example.gov.in/SevaSindhuEntry.ashx";

    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<ITokenStatusHttpClient> _http = new();
    private readonly Mock<IAuditRepository> _audit = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Scheme _scheme = new() { Id = Guid.NewGuid(), ServiceCode = "2235", Name = "Ration card", SevasindhuServiceId = "2952" };
    private readonly Department _department = new() { Id = Guid.NewGuid(), Code = "food", Name = "Food" };
    private readonly Citizen _citizen = new() { Id = Guid.NewGuid(), SevaOneUserId = 4242, Mobile = "9000000099", DigiLockerId = "DL-1", Email = "a@b.in" };
    private readonly TokenIntegrationActiveRow _integration = new() { TypeKey = "edcs_api", LandingUrl = Landing };
    private TokenIntegrationAttemptWrite? _attempt;
    private string? _sentJson;

    public EdcsApiTokenTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _integrations.Setup(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()))
            .Callback<TokenIntegrationAttemptWrite, CancellationToken>((a, _) => _attempt = a).ReturnsAsync("EDCS295200000012");
    }

    private EdcsApiTokenService Service(string? baseUrl = Api) =>
        new(_integrations.Object, _http.Object, _audit.Object, _clock.Object, Options.Create(new EdcsApiOptions { BaseUrl = baseUrl }), NullLogger<EdcsApiTokenService>.Instance);

    private void ApiReplies(int status, string body) =>
        _http.Setup(h => h.PostJsonAsync($"{Api}/generate", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TokenStatusAuth, CancellationToken>((_, json, _, _) => _sentJson = json)
            .ReturnsAsync(new TokenStatusHttpResult(status, body, null));

    private const string Good = """{"AccessToken":"jwt.from.sevasindhu","TokenType":"Bearer","ExpiresIn":7199,"EdcsApplicationRefNo":"EDCS295200000012"}""";

    [Fact]
    public async Task Asks_the_api_with_the_sevasindhu_service_id_and_sends_the_browser_to_sevasindhus_own_redirect()
    {
        ApiReplies(200, Good);

        var outcome = await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        var sent = JsonDocument.Parse(_sentJson!).RootElement;
        sent.GetProperty("UserId").GetString().Should().Be("4242");
        sent.GetProperty("ServiceId").GetInt32().Should().Be(2952);
        sent.GetProperty("AssistedMode").GetString().Should().Be("N");
        sent.GetProperty("Mobile").GetString().Should().Be("9000000099");

        // Seva Sindhu's own /redirect reads the service id out of the token and sends the citizen on to the right
        // department page itself, so we never need a department URL of our own.
        outcome.PostForm.Should().BeNull();
        outcome.ApplyUrl.Should().Be($"{Api}/redirect?token=jwt.from.sevasindhu");
    }

    [Fact]
    public async Task Records_the_reference_sevasindhu_issued_and_never_keeps_the_token()
    {
        ApiReplies(200, Good);

        await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        _attempt!.EdcsApplicationRefNo.Should().Be("EDCS295200000012");
        _attempt.TokenId.Should().BeNull();
        _attempt.SentFields.Should().NotContain("jwt.from.sevasindhu").And.Contain("\"ServiceId\":\"2952\"");
        _attempt.LandingUrl.Should().Be($"{Api}/redirect?token=jwt.from.sevasindhu");
        _attempt.ExpiresAt.Should().Be(Now.AddSeconds(7199));
    }

    [Fact]
    public async Task Sends_every_identity_field_the_citizen_has_whatever_the_service_toggles_say()
    {
        ApiReplies(200, Good);

        await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        var sent = JsonDocument.Parse(_sentJson!).RootElement;
        sent.GetProperty("Mobile").GetString().Should().Be("9000000099");
        sent.GetProperty("DigiLockerId").GetString().Should().Be("DL-1");
        sent.GetProperty("EmailId").GetString().Should().Be("a@b.in");
    }

    [Fact]
    public async Task Key1_is_the_aadhaar_hash_only_when_the_admin_picks_it()
    {
        ApiReplies(200, Good);
        _citizen.AadhaarHash = "stored-hash-123";

        await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        JsonDocument.Parse(_sentJson!).RootElement.TryGetProperty("Key1", out _).Should().BeFalse();

        ApiReplies(200, Good);
        _integration.ExtraConfig = """{"edcsKeys":{"key1":"{citizen:aadhaarHash}"}}""";
        await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        JsonDocument.Parse(_sentJson!).RootElement.GetProperty("Key1").GetString().Should().Be("stored-hash-123");
    }

    [Fact]
    public async Task Each_key_can_be_a_fixed_value_a_citizen_field_or_the_citizens_answer()
    {
        ApiReplies(200, Good);
        _citizen.District = "Mysuru";
        _integration.ExtraConfig = """{"edcsKeys":{"key1":"{citizen:mobile}","key2":"fixed-2","key3":"{citizen:district}","key4":"{ask:Ration card number}"}}""";

        var outcome = await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None,
            new Dictionary<string, string> { ["Key4"] = "  RC-123 " });

        outcome.NeedsInput.Should().BeNull();
        var sent = JsonDocument.Parse(_sentJson!).RootElement;
        sent.GetProperty("Key1").GetString().Should().Be("9000000099");
        sent.GetProperty("Key2").GetString().Should().Be("fixed-2");
        sent.GetProperty("Key3").GetString().Should().Be("Mysuru");
        sent.GetProperty("Key4").GetString().Should().Be("RC-123");
    }

    [Fact]
    public async Task An_unanswered_question_is_asked_first_and_no_token_is_made()
    {
        ApiReplies(200, Good);
        _integration.ExtraConfig = """{"edcsKeys":{"key4":"{ask:Ration card number}"}}""";

        var outcome = await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        outcome.NeedsInput.Should().ContainSingle().Which.Should().Be(new ApplyInputRequestDto("Key4", "Ration card number"));
        outcome.ApplyUrl.Should().BeNull();
        _sentJson.Should().BeNull();
    }

    [Theory]
    [InlineData("{citizen:nope}", true)]
    [InlineData("{ask:}", true)]
    [InlineData("{citizen:aadhaarHash}", false)]
    [InlineData("plain text", false)]
    public void Admin_input_is_checked(string raw, bool invalid) =>
        (EdcsKeyValue.Validate(raw, "Key1") is not null).Should().Be(invalid);

    [Fact]
    public async Task Sends_key1_through_key5_only_when_the_service_has_them_set()
    {
        ApiReplies(200, Good);
        _integration.ExtraConfig = """{"edcsKeys":{"key1":"aadhaar-hash-abc","key3":"some-other-value"}}""";

        await Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        var sent = JsonDocument.Parse(_sentJson!).RootElement;
        sent.GetProperty("Key1").GetString().Should().Be("aadhaar-hash-abc");
        sent.GetProperty("Key3").GetString().Should().Be("some-other-value");
        sent.TryGetProperty("Key2", out _).Should().BeFalse();
        sent.TryGetProperty("Key4", out _).Should().BeFalse();
        sent.TryGetProperty("Key5", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public async Task Refuses_when_the_service_has_no_sevasindhu_service_id(string? id)
    {
        _scheme.SevasindhuServiceId = id;
        ApiReplies(200, Good);

        var act = () => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _integrations.Verify(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refuses_when_the_api_address_is_not_configured()
    {
        var act = () => Service(baseUrl: null).StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Theory]
    [InlineData(400, """{"errors":{"AssistedMode":["AssistedMode must be Y or N."]}}""")]
    [InlineData(500, "boom")]
    [InlineData(200, "not json")]
    [InlineData(200, """{"AccessToken":"x"}""")]
    public async Task Refuses_and_records_nothing_when_the_api_fails_or_answers_badly(int status, string body)
    {
        ApiReplies(status, body);

        var act = () => Service().StartAsync(_citizen, _scheme, _department, _integration, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _integrations.Verify(i => i.CreateAttemptAsync(It.IsAny<TokenIntegrationAttemptWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
