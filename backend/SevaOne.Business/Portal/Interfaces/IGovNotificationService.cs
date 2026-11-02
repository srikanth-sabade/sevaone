using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IGovNotificationService
{
    Task<IReadOnlyList<PublicGovNotificationDto>> GetPublicAsync(CancellationToken ct);
    Task<IReadOnlyList<GovNotificationDto>> ListForAdminAsync(CancellationToken ct);
    Task<GovNotificationDto> CreateAsync(CreateGovNotificationRequest request, CancellationToken ct);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct);
}
