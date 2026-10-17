using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Discovery;

/// <summary>Data access for catalog role visibility grants/revokes and the service change log — split out from <see cref="IDiscoveryRepository"/> to keep that interface from growing unbounded while staying in the same discovery bounded context.</summary>
public interface ICatalogVisibilityRepository
{
    Task<IReadOnlyList<CatalogRole>> ListCatalogRolesAsync(CancellationToken ct);

    /// <summary>grant=true inserts/refreshes the visibility row; grant=false deletes it. Always call InsertServiceChangeLogAsync alongside this — the function itself does not log.</summary>
    Task SetServiceVisibilityAsync(Guid serviceId, Guid roleId, bool grant, string grantedBy, IUnitOfWork? uow, CancellationToken ct);

    Task<IReadOnlyList<ServiceVisibilityRow>> ListServiceVisibilityAsync(Guid serviceId, CancellationToken ct);

    Task<Guid> InsertServiceChangeLogAsync(ServiceChangeLog entry, IUnitOfWork? uow, CancellationToken ct);

    /// <summary>serviceId=null returns the whole catalogue's audit log (most-recent first).</summary>
    Task<IReadOnlyList<ServiceChangeLog>> ListServiceChangeLogAsync(Guid? serviceId, int limit, CancellationToken ct);
}

public sealed class ServiceVisibilityRow
{
    public string RoleKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTimeOffset GrantedAt { get; set; }
    public string? GrantedBy { get; set; }
}
