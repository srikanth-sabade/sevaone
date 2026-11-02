using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Status;

/// <summary>Data access for the status.* schema: application timeline events and department fee-payment status callbacks.</summary>
public interface IStatusRepository
{
    Task InsertApplicationEventAsync(ApplicationEvent applicationEvent, CancellationToken ct);
    Task<IReadOnlyList<ApplicationEvent>> GetTimelineAsync(Guid applicationId, CancellationToken ct);

    Task<PaymentStatusPost> InsertPaymentStatusPostAsync(PaymentStatusPost post, CancellationToken ct);
    Task<PaymentStatusPost?> GetPaymentStatusByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct);
}
