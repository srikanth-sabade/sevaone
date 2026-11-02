using FluentAssertions;
using SevaOne.Business.Stages.Packages;
using SevaOne.Business.Stages.Runtime;

namespace SevaOne.UnitTests.Stages;

public class StageEngineTests
{
    // ---- the pilot journey -------------------------------------------------------------------

    [Fact]
    public void Start_opens_the_first_stage_only()
    {
        var s = new Scenario();

        s.Case.Status.Should().Be(CaseStatus.InProgress);
        s.Case.PackageVersion.Should().Be(1);
        s.Case.Stages.Should().HaveCount(1);
        s.Case.Current.StageId.Should().Be("application");
        s.Case.Current.Status.Should().Be(StageStatus.Open);
    }

    [Fact]
    public void Submitting_a_stage_with_a_fee_asks_for_payment_before_anything_else()
    {
        var s = new Scenario();

        s.Submit("""{"name":"Asha"}""");

        s.Case.Current.Status.Should().Be(StageStatus.FeeDue);
        s.Case.Current.FormJson.Should().Be("""{"name":"Asha"}""");
        s.Case.Current.FeeReference.Should().Be(StageEngine.BuildFeeReference(s.Case.Id, "application", 1));

        var requested = s.Events.Single(e => e.Type == EngineEventType.FeeRequested);
        requested.Data!["amount"].Should().Be("30");
        requested.Data["gateway"].Should().Be("paygov");
        requested.Data["feeReference"].Should().Be(s.Case.Current.FeeReference);
    }

    [Fact]
    public void A_confirmed_payment_hands_the_stage_to_the_officer_with_an_sla_deadline()
    {
        var s = new Scenario();
        s.Submit();

        s.Pay();

        s.Case.Current.Status.Should().Be(StageStatus.AwaitingApproval);
        s.Case.Current.PaymentReference.Should().Be("PAY-1");
        s.Case.Current.ApprovalDueAt.Should().Be(Pilot.T0.AddDays(7));
        s.Events.Last().Type.Should().Be(EngineEventType.ApprovalRequested);
        s.Events.Last().Data!["role"].Should().Be("tahsildar");
    }

    [Fact]
    public void Approval_unlocks_stage_two_and_starts_its_applicant_window()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();
        s.Now = Pilot.T0.AddDays(2);

        s.Decide(Decision.Approve, Pilot.Tahsildar);

        s.Case.Status.Should().Be(CaseStatus.InProgress);
        s.Case.CurrentStage.Should().Be(1);
        s.Case.Stages[0].Status.Should().Be(StageStatus.Approved);
        s.Case.Current.StageId.Should().Be("verification");
        s.Case.Current.Status.Should().Be(StageStatus.Open);
        s.Case.Current.ExpiresAt.Should().Be(Pilot.T0.AddDays(2 + 30));
    }

    [Fact]
    public void A_stage_without_a_fee_goes_straight_to_approval()
    {
        var s = new Scenario();
        s.CompleteStageOne();

        var submit = s.Submit();

        s.Case.Current.Status.Should().Be(StageStatus.AwaitingApproval);
        submit.Events.Select(e => e.Type).Should().Equal(EngineEventType.StageSubmitted, EngineEventType.ApprovalRequested);
        submit.Events[1].Data!["role"].Should().Be("revenue-officer");
    }

    [Fact]
    public void The_full_two_stage_journey_issues_the_case_and_emits_events_in_order()
    {
        var s = new Scenario();
        s.CompleteStageOne();
        s.Submit();

        s.Decide(Decision.Approve, Pilot.RevenueOfficer);

        s.Case.Status.Should().Be(CaseStatus.Issued);
        s.Events.Select(e => e.Type).Should().Equal(
            EngineEventType.CaseStarted,
            EngineEventType.StageSubmitted, EngineEventType.FeeRequested,
            EngineEventType.FeePaid, EngineEventType.ApprovalRequested,
            EngineEventType.StageApproved, EngineEventType.StageUnlocked,
            EngineEventType.StageSubmitted, EngineEventType.ApprovalRequested,
            EngineEventType.StageApproved, EngineEventType.CaseIssued);
    }

    [Fact]
    public void Stage_two_cannot_be_submitted_while_stage_one_waits_for_an_officer()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();

        var act = () => s.Engine.Submit(s.Package, s.Case, "{}", s.Now);

        act.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.InvalidState);
        s.Case.Stages.Should().HaveCount(1, "stage two does not exist until stage one is approved");
    }

    [Fact]
    public void A_one_stage_service_with_no_fee_or_approval_issues_on_submit()
    {
        var package = new ServicePackage("simple", 1, "Simple", [new StageDefinition("apply", "Apply", null, null, null, null)]);
        var s = new Scenario(package);

        s.Submit();

        s.Case.Status.Should().Be(CaseStatus.Issued);
        s.Events.Select(e => e.Type).Should().Equal(
            EngineEventType.CaseStarted, EngineEventType.StageSubmitted,
            EngineEventType.StageApproved, EngineEventType.CaseIssued);
    }

    // ---- payments ----------------------------------------------------------------------------

    [Fact]
    public void A_failed_payment_can_be_retried_under_a_new_reference()
    {
        var s = new Scenario();
        s.Submit();
        var first = s.Case.Current.FeeReference;

        s.Pay(PaymentOutcome.Failed);
        s.Case.Current.Status.Should().Be(StageStatus.FeeFailed);

        s.Retry();
        s.Case.Current.Status.Should().Be(StageStatus.FeeDue);
        s.Case.Current.FeeReference.Should().NotBe(first).And.EndWith("-2");

        s.Pay(paymentReference: "PAY-2");
        s.Case.Current.Status.Should().Be(StageStatus.AwaitingApproval);
    }

    [Fact]
    public void A_retry_is_only_possible_after_a_failure()
    {
        var s = new Scenario();
        s.Submit();

        var act = () => s.Engine.RetryFee(s.Package, s.Case, s.Now);

        act.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.InvalidState);
    }

    [Fact]
    public void A_repeated_paid_callback_changes_nothing()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();
        var before = s.Case;
        var eventCount = s.Events.Count;

        var again = s.Pay();

        again.Events.Should().BeEmpty();
        s.Case.Should().BeSameAs(before);
        s.Events.Should().HaveCount(eventCount);
    }

    [Fact]
    public void A_second_different_payment_is_reported_for_refund_without_touching_state()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay(paymentReference: "PAY-1");
        var before = s.Case;

        var second = s.Pay(paymentReference: "PAY-OTHER");

        s.Case.Should().BeSameAs(before);
        second.Events.Should().ContainSingle().Which.Type.Should().Be(EngineEventType.DuplicatePayment);
        second.Events[0].Data!["paymentReference"].Should().Be("PAY-OTHER");
    }

    [Fact]
    public void A_failure_arriving_after_payment_is_ignored()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();
        var before = s.Case;

        var stale = s.Pay(PaymentOutcome.Failed);

        stale.Events.Should().BeEmpty();
        s.Case.Should().BeSameAs(before);
    }

    [Fact]
    public void A_late_success_for_an_earlier_attempt_is_honoured_and_the_newer_one_is_flagged_duplicate()
    {
        var s = new Scenario();
        s.Submit();
        var attemptOne = s.Case.Current.FeeReference!;
        s.Pay(PaymentOutcome.Failed);
        s.Retry();
        var attemptTwo = s.Case.Current.FeeReference!;

        s.Pay(feeReference: attemptOne, paymentReference: "PAY-LATE");
        s.Case.Current.Status.Should().Be(StageStatus.AwaitingApproval);
        s.Case.Current.PaymentReference.Should().Be("PAY-LATE");

        var dup = s.Pay(feeReference: attemptTwo, paymentReference: "PAY-NEW");
        dup.Events.Should().ContainSingle().Which.Type.Should().Be(EngineEventType.DuplicatePayment);
    }

    [Fact]
    public void Money_arriving_after_the_stage_expired_is_reported_as_orphaned()
    {
        var s = new Scenario(Pilot.PackageWithStageOneExpiry());
        s.Submit();
        s.Now = Pilot.T0.AddDays(6);
        s.Expire();
        s.Case.Status.Should().Be(CaseStatus.Expired);
        var before = s.Case;

        var late = s.Pay();

        s.Case.Should().BeSameAs(before);
        late.Events.Should().ContainSingle().Which.Type.Should().Be(EngineEventType.OrphanedPayment);
    }

    [Theory]
    [InlineData("not-a-reference")]
    [InlineData("00000000000000000000000000000000-application-1")]
    public void A_fee_reference_from_nowhere_is_rejected(string reference)
    {
        var s = new Scenario();
        s.Submit();

        var act = () => s.Engine.RecordPayment(s.Package, s.Case, reference, PaymentOutcome.Paid, "PAY-1", s.Now);

        act.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.UnknownFeeReference);
    }

    [Fact]
    public void An_attempt_number_that_was_never_issued_is_rejected()
    {
        var s = new Scenario();
        s.Submit();
        var neverIssued = StageEngine.BuildFeeReference(s.Case.Id, "application", 9);

        var act = () => s.Engine.RecordPayment(s.Package, s.Case, neverIssued, PaymentOutcome.Paid, "PAY-1", s.Now);

        act.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.UnknownFeeReference);
    }

    // ---- officer decisions -------------------------------------------------------------------

    [Fact]
    public void An_officer_without_the_stage_role_cannot_decide()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();

        var act = () => s.Engine.Decide(s.Package, s.Case, Decision.Approve, "officer-9", Pilot.RevenueOfficer, null, s.Now);

        act.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.NotAuthorized);
        s.Case.Current.Status.Should().Be(StageStatus.AwaitingApproval);
    }

    [Fact]
    public void Role_matching_ignores_case()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();

        s.Decide(Decision.Approve, ["TAHSILDAR"]);

        s.Case.CurrentStage.Should().Be(1);
    }

    [Theory]
    [InlineData(Decision.Return)]
    [InlineData(Decision.Reject)]
    public void Returning_or_rejecting_needs_a_reason(Decision decision)
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();

        var act = () => s.Engine.Decide(s.Package, s.Case, decision, "officer-1", Pilot.Tahsildar, "  ", s.Now);

        act.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.NoteRequired);
    }

    [Fact]
    public void A_returned_stage_resubmits_without_paying_the_fee_again()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();

        s.Decide(Decision.Return, Pilot.Tahsildar, "Income proof is unreadable");
        s.Case.Current.Status.Should().Be(StageStatus.Returned);
        s.Case.Current.DecisionNote.Should().Be("Income proof is unreadable");
        s.Events.Last().Type.Should().Be(EngineEventType.StageReturned);

        var resubmit = s.Submit("""{"fixed":true}""");

        s.Case.Current.Status.Should().Be(StageStatus.AwaitingApproval);
        s.Case.Current.DecisionNote.Should().BeNull("the previous decision is cleared when the applicant resubmits");
        resubmit.Events.Select(e => e.Type).Should().NotContain(EngineEventType.FeeRequested);
    }

    [Fact]
    public void A_returned_stage_gives_the_applicant_a_fresh_window_and_an_officer_delay_does_not_eat_it()
    {
        var s = new Scenario();
        s.CompleteStageOne();
        s.Now = Pilot.T0.AddDays(10);
        s.Submit();
        s.Now = Pilot.T0.AddDays(12);

        s.Decide(Decision.Return, Pilot.RevenueOfficer, "Attach the ration card", "officer-2");

        s.Case.Current.ExpiresAt.Should().Be(Pilot.T0.AddDays(12 + 30));
    }

    [Fact]
    public void Rejection_is_terminal()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();

        s.Decide(Decision.Reject, Pilot.Tahsildar, "Not a resident");

        s.Case.Status.Should().Be(CaseStatus.Rejected);
        s.Case.Current.Status.Should().Be(StageStatus.Rejected);
        s.Events.Last().Type.Should().Be(EngineEventType.CaseRejected);
        s.Events.Last().Data!["note"].Should().Be("Not a resident");

        var submit = () => s.Engine.Submit(s.Package, s.Case, "{}", s.Now);
        var decide = () => s.Engine.Decide(s.Package, s.Case, Decision.Approve, "o", Pilot.Tahsildar, null, s.Now);
        submit.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.InvalidState);
        decide.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.InvalidState);
    }

    [Fact]
    public void Deciding_a_stage_that_is_not_awaiting_approval_is_refused()
    {
        var s = new Scenario();

        var act = () => s.Engine.Decide(s.Package, s.Case, Decision.Approve, "o", Pilot.Tahsildar, null, s.Now);

        act.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.InvalidState);
    }

    // ---- time --------------------------------------------------------------------------------

    [Fact]
    public void An_unfinished_stage_expires_once_the_applicant_window_lapses_and_not_before()
    {
        var s = new Scenario();
        s.CompleteStageOne();
        var unlocked = s.Case;

        s.Now = Pilot.T0.AddDays(29);
        s.Expire().Case.Should().BeSameAs(unlocked);

        s.Now = Pilot.T0.AddDays(30);
        s.Expire();

        s.Case.Status.Should().Be(CaseStatus.Expired);
        s.Case.Current.Status.Should().Be(StageStatus.Expired);
        s.Events.Last().Type.Should().Be(EngineEventType.StageExpired);
    }

    [Fact]
    public void A_stage_waiting_on_an_officer_never_expires()
    {
        var s = new Scenario();
        s.CompleteStageOne();
        s.Submit();
        var waiting = s.Case;

        s.Now = Pilot.T0.AddDays(90);
        s.Expire();

        s.Case.Should().BeSameAs(waiting);
        s.Case.Current.Status.Should().Be(StageStatus.AwaitingApproval);
    }

    [Fact]
    public void An_approval_past_its_sla_is_flagged_without_changing_state()
    {
        var s = new Scenario();
        s.Submit();
        s.Pay();

        StageEngine.IsApprovalOverdue(s.Case, Pilot.T0.AddDays(6)).Should().BeFalse();
        StageEngine.IsApprovalOverdue(s.Case, Pilot.T0.AddDays(8)).Should().BeTrue();
        s.Case.Current.Status.Should().Be(StageStatus.AwaitingApproval);
    }

    // ---- version pinning ---------------------------------------------------------------------

    [Fact]
    public void A_case_refuses_to_run_against_a_different_package_version()
    {
        var s = new Scenario();
        var v2 = s.Package with { Version = 2 };

        var act = () => s.Engine.Submit(v2, s.Case, "{}", s.Now);

        act.Should().Throw<StageEngineException>().Which.Code.Should().Be(EngineErrorCode.PackageMismatch);
    }

    [Fact]
    public void Every_state_change_bumps_the_revision()
    {
        var s = new Scenario();
        var r0 = s.Case.Revision;

        s.Submit();
        var r1 = s.Case.Revision;
        s.Pay();
        var r2 = s.Case.Revision;

        r1.Should().BeGreaterThan(r0);
        r2.Should().BeGreaterThan(r1);
    }
}
