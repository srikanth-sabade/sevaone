using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IServiceNoticeService
{
    Task<PublicServiceNoticeDto?> GetPublicAsync(CancellationToken ct);
    Task<ServiceNoticeDto?> GetForAdminAsync(CancellationToken ct);
    Task<IReadOnlyList<ServiceNoticeHistoryDto>> GetHistoryAsync(CancellationToken ct);
    Task<ServiceNoticeDto> SaveAsync(SaveServiceNoticeRequest request, CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}
