using System.Globalization;
using SevaOne.Business.Stages.Packages;

namespace SevaOne.Business.Stages.Runtime;

/// <summary>
/// The state machine behind a multi-stage service. It is deliberately pure: every method takes the
/// package, the current case and "now", and returns a new case plus the events that describe what
/// happened. No I/O, no clock, no hidden state -- persistence, locking and delivery belong to the
/// caller, which is what makes every rule here testable without a database.
///
/// Per stage the flow is: Open -> (submit) -> [FeeDue -> (paid)] -> [AwaitingApproval -> (approve)]
/// -> Approved, which unlocks the next stage or, on the last stage, issues the case. Fee and
/// approval are each optional per stage. Waits are never polled: a fee wait ends on a gateway
/// callback, an approval wait on an officer's decision, and expiry on <see cref="Expire"/>.
/// </summary>
public sealed class StageEngine
{
    public Transition Start(ServicePackage package, Guid caseId, string applicantId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(applicantId))
        {
            throw new ArgumentException("An applicant is required.", nameof(applicantId));
        }

        var first = package.Stages[0];
        var started = new CaseInstance(
            caseId, package.ServiceCode, package.Version, applicantId,
            CaseStatus.InProgress, CurrentStage: 0, [OpenStage(first, now)], Revision: 1);

        return new Transition(started, [Event(EngineEventType.CaseStarted, started, first.Id, now, ("version", package.Version.ToString(CultureInfo.InvariantCulture)))]);
    }

    /// <summary>The applicant submits the current stage (first time, or again after a return).</summary>
    public Transition Submit(ServicePackage package, CaseInstance c, string formJson, DateTimeOffset now)
    {
        CheckPackage(package, c);
        RequireInProgress(c);

        var stage = c.Current;
        if (stage.Status is not (StageStatus.Open or StageStatus.Returned))
        {
            throw Invalid($"Stage '{stage.StageId}' is {stage.Status} and cannot be submitted now.");
        }

        var def = package.Stages[c.CurrentStage];
        var events = new List<EngineEvent> { Event(EngineEventType.StageSubmitted, c, stage.StageId, now) };
        stage = stage with { FormJson = formJson, DecidedBy = null, DecisionNote = null };

        // A fee already paid for this stage is not charged again when an officer returns it for correction.
        if (def.Fee is { } fee && !stage.FeePaid)
        {
            return RequestFee(c, stage, fee, events, now);
        }

        return AfterFee(package, c, stage, events, now);
    }

    /// <summary>
    /// A gateway callback. Safe to deliver more than once and out of order: duplicates and stale
    /// results change nothing, and money that arrives with nowhere to go is reported as an event
    /// (for refund) rather than silently dropped or applied to the wrong state.
    /// </summary>
    public Transition RecordPayment(
        ServicePackage package, CaseInstance c, string feeReference, PaymentOutcome outcome,
        string paymentReference, DateTimeOffset now)
    {
        CheckPackage(package, c);

        if (!TryParseFeeReference(c.Id, feeReference, out var stageId, out var attempt))
        {
            throw new StageEngineException(EngineErrorCode.UnknownFeeReference, $"'{feeReference}' is not a fee reference of case {c.Id}.");
        }

        var index = c.Stages.ToList().FindIndex(s => s.StageId == stageId);
        if (index < 0 || attempt < 1 || attempt > c.Stages[index].FeeAttempt)
        {
            throw new StageEngineException(EngineErrorCode.UnknownFeeReference, $"'{feeReference}' was never issued for case {c.Id}.");
        }

        var stage = c.Stages[index];

        if (outcome == PaymentOutcome.Failed)
        {
            // Only the live attempt's failure matters; a stale one, or a failure after payment, is noise.
            if (stage.FeePaid || index != c.CurrentStage || c.Status != CaseStatus.InProgress
                || stage.Status != StageStatus.FeeDue || attempt != stage.FeeAttempt)
            {
                return NoChange(c);
            }

            return Commit(
                c, stage with { Status = StageStatus.FeeFailed },
                [Event(EngineEventType.FeeFailed, c, stageId, now, ("feeReference", feeReference))]);
        }

        if (stage.FeePaid)
        {
            return stage.PaymentReference == paymentReference
                ? NoChange(c)
                : Report(c, EngineEventType.DuplicatePayment, stageId, now, feeReference, paymentReference);
        }

        var collecting = c.Status == CaseStatus.InProgress
            && index == c.CurrentStage
            && stage.Status is StageStatus.FeeDue or StageStatus.FeeFailed;
        if (!collecting)
        {
            return Report(c, EngineEventType.OrphanedPayment, stageId, now, feeReference, paymentReference);
        }

        var paid = stage with { PaymentReference = paymentReference };
        var events = new List<EngineEvent>
        {
            Event(EngineEventType.FeePaid, c, stageId, now, ("feeReference", feeReference), ("paymentReference", paymentReference)),
        };
        return AfterFee(package, c, paid, events, now);
    }

    /// <summary>The applicant tries again after a failed fee attempt. Each attempt gets its own reference.</summary>
    public Transition RetryFee(ServicePackage package, CaseInstance c, DateTimeOffset now)
    {
        CheckPackage(package, c);
        RequireInProgress(c);

        var stage = c.Current;
        if (stage.Status != StageStatus.FeeFailed || package.Stages[c.CurrentStage].Fee is not { } fee)
        {
            throw Invalid($"Stage '{stage.StageId}' is {stage.Status}; there is no failed fee to retry.");
        }

        return RequestFee(c, stage, fee, [], now);
    }

    /// <summary>An officer decides a stage that is awaiting approval.</summary>
    public Transition Decide(
        ServicePackage package, CaseInstance c, Decision decision, string officerId,
        IReadOnlyCollection<string> officerRoles, string? note, DateTimeOffset now)
    {
        CheckPackage(package, c);
        RequireInProgress(c);

        var def = package.Stages[c.CurrentStage];
        var stage = c.Current;
        if (stage.Status != StageStatus.AwaitingApproval || def.Approval is not { } approval)
        {
            throw Invalid($"Stage '{stage.StageId}' is {stage.Status}; it is not awaiting a decision.");
        }

        if (!officerRoles.Contains(approval.Role, StringComparer.OrdinalIgnoreCase))
        {
            throw new StageEngineException(EngineErrorCode.NotAuthorized, $"Deciding stage '{stage.StageId}' needs the '{approval.Role}' role.");
        }

        if (decision != Decision.Approve && string.IsNullOrWhiteSpace(note))
        {
            throw new StageEngineException(EngineErrorCode.NoteRequired, "A reason is required when returning or rejecting a stage.");
        }

        stage = stage with { DecidedBy = officerId, DecisionNote = note, ApprovalDueAt = null };

        switch (decision)
        {
            case Decision.Approve:
                return Advance(package, c, stage, [], now);

            case Decision.Return:
                // The applicant gets a fresh window to correct, because the clock was paused while an officer held it.
                var window = def.ExpiresAfterDays is { } days ? now.AddDays(days) : (DateTimeOffset?)null;
                return Commit(
                    c, stage with { Status = StageStatus.Returned, ExpiresAt = window },
                    [Event(EngineEventType.StageReturned, c, stage.StageId, now, ("by", officerId), ("note", note!))]);

            default:
                return Close(
                    c, stage with { Status = StageStatus.Rejected }, CaseStatus.Rejected,
                    Event(EngineEventType.CaseRejected, c, stage.StageId, now, ("by", officerId), ("note", note!)));
        }
    }

    /// <summary>
    /// Expires the current stage if the applicant let their window lapse. Only waits that depend on
    /// the applicant can expire; a stage sitting with an officer never does.
    /// </summary>
    public Transition Expire(ServicePackage package, CaseInstance c, DateTimeOffset now)
    {
        CheckPackage(package, c);
        if (c.Status != CaseStatus.InProgress)
        {
            return NoChange(c);
        }

        var stage = c.Current;
        var applicantWait = stage.Status is StageStatus.Open or StageStatus.Returned or StageStatus.FeeDue or StageStatus.FeeFailed;
        if (!applicantWait || stage.ExpiresAt is not { } deadline || deadline > now)
        {
            return NoChange(c);
        }

        return Close(
            c, stage with { Status = StageStatus.Expired }, CaseStatus.Expired,
            Event(EngineEventType.StageExpired, c, stage.StageId, now, ("deadline", deadline.ToString("O", CultureInfo.InvariantCulture))));
    }

    /// <summary>True when the stage has been with an officer longer than its SLA. Reporting only; nothing changes state.</summary>
    public static bool IsApprovalOverdue(CaseInstance c, DateTimeOffset now) =>
        c.Status == CaseStatus.InProgress
        && c.Current is { Status: StageStatus.AwaitingApproval, ApprovalDueAt: { } due }
        && due < now;

    /// <summary>
    /// Deterministic, so initiating the same attempt twice yields the same reference and the gateway
    /// can de-duplicate. Encodes the stage and attempt so a late callback for an older attempt is
    /// still recognised.
    /// </summary>
    public static string BuildFeeReference(Guid caseId, string stageId, int attempt) =>
        $"{caseId:N}-{stageId}-{attempt}";

    private static Transition RequestFee(CaseInstance c, StageInstance stage, FeeRule fee, List<EngineEvent> events, DateTimeOffset now)
    {
        var attempt = stage.FeeAttempt + 1;
        var reference = BuildFeeReference(c.Id, stage.StageId, attempt);
        events.Add(Event(
            EngineEventType.FeeRequested, c, stage.StageId, now,
            ("amount", fee.Amount.ToString(CultureInfo.InvariantCulture)), ("gateway", fee.Gateway), ("feeReference", reference)));

        return Commit(c, stage with { Status = StageStatus.FeeDue, FeeAttempt = attempt, FeeReference = reference }, events);
    }

    /// <summary>Fee satisfied, or none applies: hand to an officer if the stage has an approval gate, else complete it.</summary>
    private static Transition AfterFee(ServicePackage package, CaseInstance c, StageInstance stage, List<EngineEvent> events, DateTimeOffset now)
    {
        if (package.Stages[c.CurrentStage].Approval is { } approval)
        {
            var due = now.AddDays(approval.SlaDays);
            events.Add(Event(
                EngineEventType.ApprovalRequested, c, stage.StageId, now,
                ("role", approval.Role), ("dueAt", due.ToString("O", CultureInfo.InvariantCulture))));

            return Commit(c, stage with { Status = StageStatus.AwaitingApproval, ApprovalDueAt = due }, events);
        }

        return Advance(package, c, stage, events, now);
    }

    /// <summary>The stage is approved: unlock the next one, or issue the case if this was the last.</summary>
    private static Transition Advance(ServicePackage package, CaseInstance c, StageInstance stage, List<EngineEvent> events, DateTimeOffset now)
    {
        events.Add(stage.DecidedBy is null
            ? Event(EngineEventType.StageApproved, c, stage.StageId, now)
            : Event(EngineEventType.StageApproved, c, stage.StageId, now, ("by", stage.DecidedBy)));

        var stages = c.Stages.ToList();
        stages[c.CurrentStage] = stage with { Status = StageStatus.Approved, ApprovalDueAt = null };

        var next = c.CurrentStage + 1;
        if (next >= package.Stages.Count)
        {
            events.Add(Event(EngineEventType.CaseIssued, c, stage.StageId, now));
            return new Transition(c with { Status = CaseStatus.Issued, Stages = stages, Revision = c.Revision + 1 }, events);
        }

        var nextDef = package.Stages[next];
        stages.Add(OpenStage(nextDef, now));
        events.Add(Event(EngineEventType.StageUnlocked, c, nextDef.Id, now));
        return new Transition(c with { CurrentStage = next, Stages = stages, Revision = c.Revision + 1 }, events);
    }

    private static Transition Commit(CaseInstance c, StageInstance stage, List<EngineEvent> events)
    {
        var stages = c.Stages.ToList();
        stages[c.CurrentStage] = stage;
        return new Transition(c with { Stages = stages, Revision = c.Revision + 1 }, events);
    }

    private static Transition Close(CaseInstance c, StageInstance stage, CaseStatus status, EngineEvent closing)
    {
        var stages = c.Stages.ToList();
        stages[c.CurrentStage] = stage;
        return new Transition(c with { Status = status, Stages = stages, Revision = c.Revision + 1 }, [closing]);
    }

    private static Transition NoChange(CaseInstance c) => new(c, []);

    /// <summary>Records an event without touching state, for money that needs a human to deal with it.</summary>
    private static Transition Report(CaseInstance c, EngineEventType type, string stageId, DateTimeOffset now, string feeReference, string paymentReference) =>
        new(c, [Event(type, c, stageId, now, ("feeReference", feeReference), ("paymentReference", paymentReference))]);

    private static StageInstance OpenStage(StageDefinition def, DateTimeOffset now) =>
        new(
            def.Id, StageStatus.Open, now,
            ExpiresAt: def.ExpiresAfterDays is { } days ? now.AddDays(days) : null,
            ApprovalDueAt: null, FeeAttempt: 0, FeeReference: null, PaymentReference: null,
            FormJson: null, DecidedBy: null, DecisionNote: null);

    private static EngineEvent Event(EngineEventType type, CaseInstance c, string stageId, DateTimeOffset at, params (string Key, string Value)[] data) =>
        new(type, c.Id, stageId, at, data.Length == 0 ? null : data.ToDictionary(d => d.Key, d => d.Value));

    private static bool TryParseFeeReference(Guid caseId, string reference, out string stageId, out int attempt)
    {
        stageId = string.Empty;
        attempt = 0;

        var prefix = caseId.ToString("N") + "-";
        if (!reference.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = reference[prefix.Length..];
        var dash = rest.LastIndexOf('-');
        if (dash <= 0)
        {
            return false;
        }

        stageId = rest[..dash];
        return int.TryParse(rest[(dash + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out attempt);
    }

    /// <summary>A case is pinned to one package version for life; refuse to run it against any other.</summary>
    private static void CheckPackage(ServicePackage package, CaseInstance c)
    {
        if (package.ServiceCode != c.ServiceCode || package.Version != c.PackageVersion)
        {
            throw new StageEngineException(
                EngineErrorCode.PackageMismatch,
                $"Case {c.Id} is pinned to {c.ServiceCode} v{c.PackageVersion}, not {package.ServiceCode} v{package.Version}.");
        }
    }

    private static void RequireInProgress(CaseInstance c)
    {
        if (c.Status != CaseStatus.InProgress)
        {
            throw Invalid($"Case {c.Id} is {c.Status}; it accepts no further actions.");
        }
    }

    private static StageEngineException Invalid(string message) => new(EngineErrorCode.InvalidState, message);
}
