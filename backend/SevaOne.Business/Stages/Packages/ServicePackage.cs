namespace SevaOne.Business.Stages.Packages;

/// <summary>
/// A published, versioned definition of a service: an ordered list of stages. This is the JSON
/// document the Designer Studio will eventually produce; the Stage Engine only ever reads it.
/// A published (ServiceCode, Version) pair is immutable -- a change is a new Version, and cases
/// already in flight stay pinned to the version they started on.
/// </summary>
public sealed record ServicePackage(
    string ServiceCode,
    int Version,
    string Name,
    IReadOnlyList<StageDefinition> Stages);

/// <summary>
/// One stage. Form, fee and approval are each optional: a stage with none of them completes as
/// soon as it is submitted. <see cref="ExpiresAfterDays"/> is the window the applicant has to act
/// once the stage opens (or is returned for correction) -- it never runs while a stage is waiting
/// on an officer.
/// </summary>
public sealed record StageDefinition(
    string Id,
    string Name,
    string? FormId,
    FeeRule? Fee,
    ApprovalRule? Approval,
    int? ExpiresAfterDays);

public sealed record FeeRule(decimal Amount, string Gateway);

/// <summary>The officer role that may decide this stage, and the days it is given to do so.</summary>
public sealed record ApprovalRule(string Role, int SlaDays);
