using FluentAssertions;
using SevaOne.Business.Stages.Packages;
using SevaOne.Business.Stages.Runtime;
using SevaOne.Business.Stages.Service;

namespace SevaOne.UnitTests.Stages;

public class StageCaseServiceTests
{
    private const string Service = "income-certificate";

    private readonly InMemoryPackageStore _packages = new();
    private readonly InMemoryCaseStore _cases = new();
    private readonly ManualClock _clock = new(Pilot.T0);
    private readonly StageCaseService _service;

    public StageCaseServiceTests()
    {
        _packages.Publish(Pilot.Package());
        _service = new StageCaseService(_cases, _packages, new StageEngine(), _clock);
    }

    /// <summary>Drives a freshly started case through both pilot stages to the end.</summary>
    private async Task<CaseInstance> RunPilotJourneyAsync(Guid caseId)
    {
        var afterSubmit = await _service.SubmitAsync(caseId, "{}");
        await _service.RecordPaymentAsync(caseId, afterSubmit.Current.FeeReference!, PaymentOutcome.Paid, "PAY-1");
        await _service.DecideAsync(caseId, Decision.Approve, "officer-1", Pilot.Tahsildar, null);
        await _service.SubmitAsync(caseId, "{}");
        return await _service.DecideAsync(caseId, Decision.Approve, "officer-2", Pilot.RevenueOfficer, null);
    }

    [Fact]
    public async Task The_pilot_journey_runs_end_to_end_through_the_service_and_fills_the_outbox_in_order()
    {
        var started = await _service.StartAsync(Service, "citizen-1");

        var finished = await RunPilotJourneyAsync(started.Id);

        finished.Status.Should().Be(CaseStatus.Issued);
        _cases.Outbox.Select(e => e.Type).Should().Equal(
            EngineEventType.CaseStarted,
            EngineEventType.StageSubmitted, EngineEventType.FeeRequested,
            EngineEventType.FeePaid, EngineEventType.ApprovalRequested,
            EngineEventType.StageApproved, EngineEventType.StageUnlocked,
            EngineEventType.StageSubmitted, EngineEventType.ApprovalRequested,
            EngineEventType.StageApproved, EngineEventType.CaseIssued);
        _cases.Outbox.Should().OnlyContain(e => e.CaseId == started.Id);
    }

    [Fact]
    public async Task A_case_in_flight_stays_on_its_version_while_new_cases_start_on_the_latest()
    {
        var inFlight = await _service.StartAsync(Service, "citizen-1");

        // Publish v2 with an extra inspection stage after the case above has already started.
        var v1 = Pilot.Package();
        var inspection = new StageDefinition("inspection", "Field inspection", null, null, new ApprovalRule("inspector", 5), null);
        _packages.Publish(v1 with { Version = 2, Stages = [.. v1.Stages, inspection] });

        var fresh = await _service.StartAsync(Service, "citizen-2");
        fresh.PackageVersion.Should().Be(2);

        var oldFinished = await RunPilotJourneyAsync(inFlight.Id);
        oldFinished.PackageVersion.Should().Be(1);
        oldFinished.Status.Should().Be(CaseStatus.Issued, "version 1 has two stages");

        var newAfterTwoStages = await RunPilotJourneyAsync(fresh.Id);
        newAfterTwoStages.PackageVersion.Should().Be(2);
        newAfterTwoStages.Status.Should().Be(CaseStatus.InProgress, "version 2 adds a third stage");
        newAfterTwoStages.Current.StageId.Should().Be("inspection");
        newAfterTwoStages.Current.Status.Should().Be(StageStatus.Open);
    }

    [Fact]
    public async Task A_duplicate_payment_still_reaches_the_outbox_even_though_the_case_did_not_change()
    {
        var started = await _service.StartAsync(Service, "citizen-1");
        var submitted = await _service.SubmitAsync(started.Id, "{}");
        var paid = await _service.RecordPaymentAsync(started.Id, submitted.Current.FeeReference!, PaymentOutcome.Paid, "PAY-1");

        var after = await _service.RecordPaymentAsync(started.Id, submitted.Current.FeeReference!, PaymentOutcome.Paid, "PAY-OTHER");

        after.Revision.Should().Be(paid.Revision);
        _cases.Outbox.Last().Type.Should().Be(EngineEventType.DuplicatePayment);
    }

    [Fact]
    public async Task The_expiry_timer_only_acts_when_the_window_has_lapsed()
    {
        var started = await _service.StartAsync(Service, "citizen-1");
        var submitted = await _service.SubmitAsync(started.Id, "{}");
        await _service.RecordPaymentAsync(started.Id, submitted.Current.FeeReference!, PaymentOutcome.Paid, "PAY-1");
        var unlocked = await _service.DecideAsync(started.Id, Decision.Approve, "officer-1", Pilot.Tahsildar, null);

        _clock.Advance(TimeSpan.FromDays(29));
        (await _service.ExpireAsync(started.Id)).Revision.Should().Be(unlocked.Revision);

        _clock.Advance(TimeSpan.FromDays(1));
        var expired = await _service.ExpireAsync(started.Id);

        expired.Status.Should().Be(CaseStatus.Expired);
    }

    [Fact]
    public async Task An_unknown_case_is_reported_as_not_found()
    {
        var act = () => _service.SubmitAsync(Guid.NewGuid(), "{}");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Saving_against_a_stale_revision_is_refused()
    {
        var started = await _service.StartAsync(Service, "citizen-1");
        var read = (await _cases.GetAsync(started.Id))!;

        // Two writers read the same revision; the first save wins.
        await _cases.SaveAsync(read with { Revision = read.Revision + 1 }, read.Revision, []);
        var second = () => _cases.SaveAsync(read with { Revision = read.Revision + 1 }, read.Revision, []);

        await second.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public void A_published_version_cannot_be_overwritten()
    {
        var act = () => _packages.Publish(Pilot.Package());

        act.Should().Throw<InvalidOperationException>().WithMessage("*already published*");
    }

    [Fact]
    public void An_invalid_package_cannot_be_published()
    {
        var broken = Pilot.Package() with { Stages = [] };

        var act = () => _packages.Publish(broken with { Version = 9 });

        act.Should().Throw<PackageValidationException>();
    }
}
