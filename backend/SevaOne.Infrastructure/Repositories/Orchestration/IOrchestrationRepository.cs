using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Orchestration;

/// <summary>Data access for the orchestration.* schema: applications, department handoff sessions, the operator wallet ledger, and the transactional outbox.</summary>
public interface IOrchestrationRepository
{
    /// <summary>Inserts the application and its first timeline event as one transaction — status.application_events has no independent existence without the application it belongs to.</summary>
    Task SubmitApplicationAsync(ServiceApplication application, ApplicationEvent firstEvent, IUnitOfWork? uow, CancellationToken ct);

    Task<ServiceApplication?> GetApplicationByIdAsync(Guid id, CancellationToken ct);
    Task<ServiceApplication?> GetApplicationByReferenceAsync(string sevaReference, CancellationToken ct);
    Task<IReadOnlyList<ServiceApplication>> GetApplicationsByCitizenAsync(Guid citizenId, CancellationToken ct);

    /// <summary>Application joined with its scheme/department names — the summary/detail/track DTOs all need these together, so this avoids an N+1 lookup per application.</summary>
    Task<ApplicationWithNames?> GetApplicationWithNamesForCitizenAsync(Guid citizenId, string reference, CancellationToken ct);

    /// <summary>Unscoped by citizen — used by Track Application, which a citizen can call with just a reference (no login required).</summary>
    Task<ApplicationWithNames?> GetApplicationWithNamesByReferenceAsync(string reference, CancellationToken ct);

    Task<IReadOnlyList<ApplicationWithNames>> ListApplicationsWithNamesForCitizenAsync(Guid citizenId, CancellationToken ct);

    /// <summary>Raw row for the department-status-callback path, which needs to mutate Status rather than just read it.</summary>
    Task<ServiceApplication?> GetApplicationByDepartmentReferenceAsync(string departmentCode, string departmentReference, CancellationToken ct);

    Task UpdateApplicationStatusAsync(Guid id, ApplicationStatus status, CancellationToken ct);

    /// <summary>Applications with a department reference that haven't reached a terminal status yet — polled by ApplicationStatusPoller to pick up department-side changes SevaOne wasn't pushed.</summary>
    Task<IReadOnlyList<InflightApplication>> GetInflightApplicationsAsync(int batchSize, CancellationToken ct);

    /// <summary>Demo/PoC citizen-merge support: reassigns every application from one citizen to another (see CitizenSessionService's seed-merge step).</summary>
    Task ReassignApplicationsAsync(Guid fromCitizenId, Guid toCitizenId, CancellationToken ct);

    Task InsertDepartmentSessionAsync(DepartmentSession session, CancellationToken ct);
    Task<DepartmentSession?> GetActiveDepartmentSessionAsync(Guid citizenId, Guid departmentId, CancellationToken ct);
    Task<IReadOnlyList<DepartmentSession>> GetActiveDepartmentSessionsByCitizenAsync(Guid citizenId, CancellationToken ct);
    Task ExpireDepartmentSessionAsync(Guid id, DateTimeOffset expiresAt, CancellationToken ct);

    Task UpsertDeptUserRefAsync(DeptUserRef reference, CancellationToken ct);

    Task InsertWalletTxnAsync(OperatorWalletTxn txn, IUnitOfWork? uow, CancellationToken ct);
    Task<OperatorWalletTxn?> GetWalletTxnByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct);

    Task InsertOutboxMessageAsync(OutboxMessage message, IUnitOfWork? uow, CancellationToken ct);
    Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(int maxAttempts, int batchSize, CancellationToken ct);
    Task MarkOutboxPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken ct);
    Task MarkOutboxFailedAsync(Guid id, int attempts, string error, CancellationToken ct);
}

/// <summary>Plain settable-property class, not a positional record — see SchemeWithDepartment's doc comment for why Dapper needs this shape.</summary>
public sealed class ApplicationWithNames
{
    public Guid Id { get; set; }
    public string SevaReference { get; set; } = "";
    public string? DepartmentReference { get; set; }
    public string ServiceCode { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset SakalaDueAt { get; set; }
    public string ApplicantName { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Purpose { get; set; }
    public string? District { get; set; }
    public string? Taluk { get; set; }
}

public sealed class InflightApplication
{
    public Guid Id { get; set; }
    public Guid CitizenId { get; set; }
    public string SevaReference { get; set; } = "";
    public string DepartmentReference { get; set; } = "";
    public string DepartmentCode { get; set; } = "";
    public int Status { get; set; }
}
