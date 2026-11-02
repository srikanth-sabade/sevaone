using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Business.Discovery.Services;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.UnitTests.Portal;

/// <summary>Fetching acknowledgements that the department reported to the Seva Sindhu API.</summary>
public class EdcsApiAckTests
{
    private const string Api = "https://sevasindhu.example/EP0115/api/TokenIntegration";
    private const string Ref = "EDCS295200000012";

    private readonly Mock<ITokenIntegrationRepository> _integrations = new();
    private readonly Mock<ITokenStatusHttpClient> _http = new();
    private readonly Mock<ITokenAcknowledgementService> _acks = new();
    private readonly TokenAckDueRow _due = new() { AttemptId = Guid.NewGuid(), EdcsApplicationRefNo = Ref, SevaOneUserId = 4242, DepartmentId = Guid.NewGuid(), DepartmentCode = "food", SevasindhuServiceId = "2952" };
    private TokenAckRequest? _recorded;
    private TokenIntegrationCaller? _caller;

    public EdcsApiAckTests()
    {
        _integrations.Setup(i => i.ListAckDueAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([_due]);
        _acks.Setup(a => a.AcknowledgeAsync(It.IsAny<TokenAckRequest>(), It.IsAny<TokenIntegrationCaller>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<TokenAckRequest, TokenIntegrationCaller, string?, CancellationToken>((r, c, _, _) => { _recorded = r; _caller = c; })
            .ReturnsAsync(new TokenAckOutcome(new TokenAckSuccess(1, Ref, "NRC-1"), null));
    }

    private EdcsApiAckService Service(string? baseUrl = Api) =>
        new(_integrations.Object, _http.Object, _acks.Object, Options.Create(new EdcsApiOptions { BaseUrl = baseUrl }), NullLogger<EdcsApiAckService>.Instance);

    private void Replies(string path, int status, string body) =>
        _http.Setup(h => h.PostJsonAsync($"{Api}/{path}", It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenStatusHttpResult(status, body, null));

    private const string File = """{"EdcsApplicationRefNo":"EDCS295200000012","AcknowledgmentFileType":"pdf","Acknowledgment":"JVBERi0xLjQ="}""";
    private const string Listed = """
        {"Count":1,"Applications":[
          {"EdcsApplicationRefNo":"EDCS295200000099","DepartmentApplicationRefNo":"OTHER"},
          {"EdcsApplicationRefNo":"EDCS295200000012","DepartmentApplicationRefNo":"NRC-1","DepartmentPaymentStatus":"Paid","DepartmentFee":10.5,
           "DepartmentFeeCollectionDate":"2026-10-28T19:00:00","DepartmentPaymentRefNumber":"PAY1","AcknowledgmentDate":"2026-10-28T19:01:00"}]}
        """;

    [Fact]
    public async Task Waits_while_the_department_has_not_reported()
    {
        Replies("acknowledgment", 404, """{"Message":"No application was found"}""");

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(0);

        _acks.Verify(a => a.AcknowledgeAsync(It.IsAny<TokenAckRequest>(), It.IsAny<TokenIntegrationCaller>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Records_the_acknowledgement_with_the_details_from_the_users_list()
    {
        Replies("acknowledgment", 200, File);
        Replies("pending-service-charges", 200, Listed);

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(1);

        _recorded!.EdcsApplicationRefNo.Should().Be(Ref);
        _recorded.UserId.Should().Be("4242");
        _recorded.DepartmentApplicationRefNo.Should().Be("NRC-1");
        _recorded.DepartmentPaymentStatus.Should().Be("Paid");
        _recorded.DepartmentFee.Should().Be(10.5m);
        _recorded.DepartmentPaymentRefNumber.Should().Be("PAY1");
        _recorded.DepartmentFeeCollectionDate.Should().Be("2026-10-28T19:00:00");
        _recorded.AcknowledgmentFileType.Should().Be("pdf");
        _recorded.Acknowledgment.Should().Be("JVBERi0xLjQ=");
        _recorded.Token.Should().BeNull();
        _caller!.DepartmentId.Should().Be(_due.DepartmentId);
        _caller.DepartmentCode.Should().Be("food");
    }

    [Fact]
    public async Task Falls_back_to_the_single_application_lookup_when_the_list_has_no_entry()
    {
        Replies("acknowledgment", 200, File);
        Replies("pending-service-charges", 200, """{"Count":0,"Applications":[]}""");
        Replies("service-charge-lookup", 200, """{"DepartmentApplicationRefNo":"NRC-7","DepartmentPaymentStatus":"Paid"}""");

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(1);

        _recorded!.DepartmentApplicationRefNo.Should().Be("NRC-7");
    }

    [Fact]
    public async Task Records_nothing_without_a_department_reference()
    {
        Replies("acknowledgment", 200, File);
        Replies("pending-service-charges", 200, """{"Count":0,"Applications":[]}""");
        Replies("service-charge-lookup", 404, """{"Message":"none"}""");

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(0);

        _acks.Verify(a => a.AcknowledgeAsync(It.IsAny<TokenAckRequest>(), It.IsAny<TokenIntegrationCaller>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Does_not_count_an_acknowledgement_that_was_not_accepted()
    {
        Replies("acknowledgment", 200, File);
        Replies("pending-service-charges", 200, Listed);
        _acks.Setup(a => a.AcknowledgeAsync(It.IsAny<TokenAckRequest>(), It.IsAny<TokenIntegrationCaller>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenAckOutcome(null, "Acknowledgment content does not match AcknowledgmentFileType"));

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task A_failing_api_is_noted_and_skipped_not_thrown()
    {
        _http.Setup(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenStatusHttpResult(null, null, "Could not reach the department's status API"));

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(0);

        _integrations.Verify(i => i.RecordAckCheckAsync(_due.AttemptId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Marks_the_check_so_a_slow_api_is_not_asked_every_pass()
    {
        Replies("acknowledgment", 404, "{}");

        await Service().PollDueAsync(20, CancellationToken.None);

        _integrations.Verify(i => i.RecordAckCheckAsync(_due.AttemptId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Does_nothing_when_the_api_address_is_not_configured()
    {
        (await Service(baseUrl: "").PollDueAsync(20, CancellationToken.None)).Should().Be(0);

        _integrations.Verify(i => i.ListAckDueAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Never_looks_up_a_reference_the_sevasindhu_api_did_not_issue()
    {
        // A local reference (our own numbering) that could coincide with someone else's application over there.
        _due.EdcsApplicationRefNo = "EDCS100100000020";
        Replies("acknowledgment", 200, File);
        Replies("pending-service-charges", 200, Listed);

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(0);

        _http.Verify(h => h.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenStatusAuth>(), It.IsAny<CancellationToken>()), Times.Never);
        _acks.Verify(a => a.AcknowledgeAsync(It.IsAny<TokenAckRequest>(), It.IsAny<TokenIntegrationCaller>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ignores_an_answer_about_a_different_reference()
    {
        Replies("acknowledgment", 200, File.Replace("EDCS295200000012", "EDCS295200000099"));
        Replies("pending-service-charges", 200, Listed);

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task Ignores_a_record_that_belongs_to_another_user()
    {
        Replies("acknowledgment", 200, File);
        Replies("pending-service-charges", 200, Listed.Replace("\"DepartmentApplicationRefNo\":\"NRC-1\"", "\"DepartmentApplicationRefNo\":\"NRC-1\",\"UserId\":\"777\""));

        (await Service().PollDueAsync(20, CancellationToken.None)).Should().Be(0);
    }
}
