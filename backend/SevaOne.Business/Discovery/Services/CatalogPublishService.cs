using System.Text.Json;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Services;

public sealed class CatalogPublishService : ICatalogPublishService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ICatalogIngestionRepository _ingestion;
    private readonly IDiscoveryRepository _discovery;
    private readonly ICatalogVisibilityRepository _visibility;
    private readonly IUnitOfWorkFactory _uowFactory;

    public CatalogPublishService(
        ICatalogIngestionRepository ingestion, IDiscoveryRepository discovery,
        ICatalogVisibilityRepository visibility, IUnitOfWorkFactory uowFactory)
    {
        _ingestion = ingestion;
        _discovery = discovery;
        _visibility = visibility;
        _uowFactory = uowFactory;
    }

    public async Task<PublishResult> PublishAsync(Guid batchId, string publishedBy, CancellationToken ct)
    {
        var batch = await _ingestion.GetImportBatchAsync(batchId, ct)
            ?? throw new NotFoundException($"Import batch '{batchId}' was not found.");
        if (batch.Status != "ready_to_publish")
        {
            throw new ConflictException($"Batch '{batchId}' is not ready to publish (status: {batch.Status}). Fix the blocking errors and re-import, or discard it.");
        }

        var rows = await _ingestion.ListImportRowsAsync(batchId, null, ct);
        var roles = await _visibility.ListCatalogRolesAsync(ct);
        var citizenRoleId = roles.FirstOrDefault(r => r.RoleKey == "citizen")?.Id;
        var operatorRoleId = roles.FirstOrDefault(r => r.RoleKey == "operator")?.Id;

        int published = 0, skipped = 0;

        // One transaction for the whole batch -- IUnitOfWorkFactory's first real caller (it was
        // registered but unused anywhere in the codebase before this). `await using` rolls back
        // automatically on any exception (UnitOfWork.DisposeAsync rolls back unless CommitAsync
        // already ran), so a partial failure never leaves the live tables half-updated.
        await using (var uow = await _uowFactory.BeginAsync(ct))
        {
            foreach (var row in rows)
            {
                // "conflict" rows never should have reached a "ready_to_publish" batch (see
                // CatalogIngestionService), but skip defensively rather than publish garbage if one did.
                if (row.Action == "conflict" || string.IsNullOrEmpty(row.NormalizedPayload))
                {
                    skipped++;
                    continue;
                }

                var normalized = JsonSerializer.Deserialize<CatalogRow>(row.NormalizedPayload, JsonOptions)
                    ?? throw new DomainException($"Row {row.RowNumber} in batch '{batchId}' has an unparseable normalized payload.");

                var departmentId = await _discovery.UpsertDepartmentAsync(
                    Guid.NewGuid(), normalized.Department.Code, normalized.Department.Name,
                    normalized.Department.HomeUrlEn, normalized.Department.HomeUrlKn, uow, ct);

                Guid? subDepartmentId = null;
                if (normalized.SubDepartment?.Name is { Length: > 0 } subName)
                {
                    subDepartmentId = await _discovery.UpsertSubDepartmentAsync(Guid.NewGuid(), departmentId, subName, uow, ct);
                }

                var request = new ServiceUpsertRequest
                {
                    Id = Guid.NewGuid(),
                    ServiceCode = normalized.Service.ServiceCode,
                    DepartmentId = departmentId,
                    SubDepartmentId = subDepartmentId,
                    Name = normalized.Service.Name,
                    NameKannada = normalized.Service.NameKannada,
                    Category = normalized.Service.Category,
                    SakalaDays = normalized.Service.SakalaDays,
                    IsSakalaEnabled = normalized.Service.IsSakalaEnabled,
                    SakalaLabelRaw = normalized.Service.SakalaLabelRaw,
                    Description = normalized.Service.Description,
                    DescriptionKannada = normalized.Service.DescriptionKannada,
                    ShortDescription = normalized.Service.ShortDescription,
                    DepartmentFee = normalized.Service.DepartmentFee,
                    ServiceCharge = normalized.Service.ServiceCharge,
                    DocumentsJson = JsonSerializer.Serialize(normalized.Service.DocumentsEn, JsonOptions),
                    DocumentsJsonKannada = JsonSerializer.Serialize(normalized.Service.DocumentsKn, JsonOptions),
                    EligibilityJson = JsonSerializer.Serialize(normalized.Service.EligibilityEn, JsonOptions),
                    EligibilityJsonKannada = JsonSerializer.Serialize(normalized.Service.EligibilityKn, JsonOptions),
                    StepsJson = JsonSerializer.Serialize(normalized.Service.ProcedureEn, JsonOptions),
                    StepsJsonKannada = JsonSerializer.Serialize(normalized.Service.ProcedureKn, JsonOptions),
                    Jurisdiction = normalized.Service.Jurisdiction,
                    ModeOfService = normalized.Service.ModeOfService,
                    DepartmentServiceId = normalized.Service.DepartmentServiceId,
                    ServiceUrl = normalized.Service.ServiceUrl,
                    DeeplinkCitizenUrl = normalized.Service.ApplyUrl,
                    DeeplinkCitizenUrlKannada = normalized.Service.ApplyUrlKannada,
                };
                var serviceId = await _discovery.UpsertServiceAsync(request, uow, ct);

                // Default visibility only for brand-new services -- an existing service keeps
                // whatever grants an admin already curated; re-importing it must never silently
                // undo an earlier revoke.
                if (row.Action == "insert")
                {
                    if (citizenRoleId is not null) await _visibility.SetServiceVisibilityAsync(serviceId, citizenRoleId.Value, true, publishedBy, uow, ct);
                    if (operatorRoleId is not null) await _visibility.SetServiceVisibilityAsync(serviceId, operatorRoleId.Value, true, publishedBy, uow, ct);
                }

                await _visibility.InsertServiceChangeLogAsync(new ServiceChangeLog
                {
                    Id = Guid.NewGuid(),
                    ServiceId = serviceId,
                    BatchId = batchId,
                    ChangedBy = publishedBy,
                    ChangeType = row.Action == "insert" ? "created" : "updated",
                }, uow, ct);

                published++;
            }

            await uow.CommitAsync(ct);
        }

        await _ingestion.UpdateImportBatchStatusAsync(batchId, "published", null, null, null, publishedBy, ct);
        return new PublishResult(published, skipped);
    }
}
