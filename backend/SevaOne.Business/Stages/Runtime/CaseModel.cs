namespace SevaOne.Business.Stages.Runtime;

public enum StageStatus
{
    /// <summary>Waiting for the applicant to fill and submit the stage's form.</summary>
    Open,
    /// <summary>Submitted; waiting for the gateway to confirm the fee.</summary>
    FeeDue,
    /// <summary>The gateway reported the fee attempt failed; the applicant may retry.</summary>
    FeeFailed,
    /// <summary>Submitted (and paid, if a fee applies); waiting for an officer's decision.</summary>
    AwaitingApproval,
    /// <summary>An officer sent it back; the applicant corrects and resubmits.</summary>
    Returned,
    Approved,
    Rejected,
    Expired,
}

public enum CaseStatus
{
    InProgress,
    Issued,
    Rejected,
    Expired,
}

public enum PaymentOutcome
{
    Paid,
    Failed,
}

public enum Decision
{
    Approve,
    Return,
    Reject,
}

/// <summary>One stage of one application. Stage instances line up index-for-index with the package's stages.</summary>
public sealed record StageInstance(
    string StageId,
    StageStatus Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? ApprovalDueAt,
    int FeeAttempt,
    string? FeeReference,
    string? PaymentReference,
    string? FormJson,
    string? DecidedBy,
    string? DecisionNote)
{
    public bool FeePaid => PaymentReference is not null;
}

/// <summary>
/// One application moving through a service's stages. Pinned to the exact package version it
/// started on. <see cref="Revision"/> goes up on every state change and is what the store uses
/// for optimistic concurrency.
/// </summary>
public sealed record CaseInstance(
    Guid Id,
    string ServiceCode,
    int PackageVersion,
    string ApplicantId,
    CaseStatus Status,
    int CurrentStage,
    IReadOnlyList<StageInstance> Stages,
    int Revision)
{
    public StageInstance Current => Stages[CurrentStage];
}

public enum EngineEventType
{
    CaseStarted,
    StageSubmitted,
    FeeRequested,
    FeePaid,
    FeeFailed,
    /// <summary>A second, different payment arrived for a fee that was already paid. Needs a refund; state is unchanged.</summary>
    DuplicatePayment,
    /// <summary>A payment arrived for a stage that is no longer collecting (expired, closed). Needs a refund; state is unchanged.</summary>
    OrphanedPayment,
    ApprovalRequested,
    StageApproved,
    StageReturned,
    StageUnlocked,
    CaseIssued,
    CaseRejected,
    StageExpired,
}

/// <summary>
/// Something that happened. The service persists these in the same transaction as the state change
/// (the existing outbox pattern), and downstream consumers -- notifications, documents, eTaal
/// reporting -- react to them instead of being called by the engine.
/// </summary>
public sealed record EngineEvent(
    EngineEventType Type,
    Guid CaseId,
    string StageId,
    DateTimeOffset At,
    IReadOnlyDictionary<string, string>? Data = null);

/// <summary>
/// The result of one engine step. When nothing changed, <see cref="Case"/> is the very same
/// instance that was passed in, so callers can detect a no-op with <see cref="ReferenceEquals"/>.
/// </summary>
public sealed record Transition(CaseInstance Case, IReadOnlyList<EngineEvent> Events);
