using System.Text.Json;
using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Reference;
using SevaOne.Infrastructure.Repositories.Portal;

namespace SevaOne.Business.Portal.Services;

public sealed class GovAdminBulkImportService : IGovAdminBulkImportService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IGovAdminImportRepository _imports;
    private readonly IGovAdminManagementService _management;
    private readonly ICurrentGovAdmin _current;

    public GovAdminBulkImportService(IGovAdminImportRepository imports, IGovAdminManagementService management, ICurrentGovAdmin current)
    {
        _imports = imports;
        _management = management;
        _current = current;
    }

    public async Task<Guid> StartImportAsync(string sourceFilename, string sourceFormat, IReadOnlyList<GovAdminImportRowRequest> rows, CancellationToken ct)
    {
        RequireSuperadmin();
        if (rows.Count is < 1 or > 5000)
        {
            throw new DomainException("Import between 1 and 5,000 admin rows.");
        }

        var batchId = Guid.NewGuid();
        await _imports.InsertBatchAsync(batchId, sourceFilename.Trim(), sourceFormat.Trim().ToLowerInvariant(), _current.DisplayName ?? "Government administrator", ct);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errorCount = 0;

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var rowId = Guid.NewGuid();
            await _imports.InsertRowAsync(rowId, batchId, index + 1, JsonSerializer.Serialize(row, Json), ct);
            var errors = Validate(row, seen);
            errorCount += errors.Count;
            await _imports.UpdateRowAsync(rowId, errors.Count == 0 ? "create" : "conflict", JsonSerializer.Serialize(errors, Json), "[]", ct);
        }

        await _imports.UpdateBatchAsync(batchId, errorCount == 0 ? "ready_to_publish" : "failed", rows.Count, errorCount, 0, null, ct);
        return batchId;
    }

    public async Task<GovAdminImportBatchDto?> GetBatchAsync(Guid batchId, CancellationToken ct)
    {
        RequireSuperadmin();
        var batch = await _imports.GetBatchAsync(batchId, ct);
        return batch is null ? null : ToDto(batch);
    }

    public async Task<IReadOnlyList<GovAdminImportRowDto>> ListRowsAsync(Guid batchId, CancellationToken ct)
    {
        RequireSuperadmin();
        var rows = await _imports.ListRowsAsync(batchId, ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<GovAdminImportPublishResult> PublishAsync(Guid batchId, CancellationToken ct)
    {
        RequireSuperadmin();
        var batch = await _imports.GetBatchAsync(batchId, ct) ?? throw new NotFoundException("Admin import batch not found.");
        if (batch.Status != "ready_to_publish")
        {
            throw new ConflictException("This import is not ready to publish. Fix validation errors or discard it.");
        }

        var rows = await _imports.ListRowsAsync(batchId, ct);
        var created = 0;
        foreach (var row in rows)
        {
            var request = JsonSerializer.Deserialize<GovAdminImportRowRequest>(row.RawPayload, Json)
                ?? throw new ConflictException($"Import row {row.RowNumber} is invalid.");
            try
            {
                await _management.CreateAsync(new CreateGovAdminRequest(
                    request.Username, request.DisplayName, request.Tier, request.AssignedDistrict,
                    request.AssignedTaluk, request.AssignedDepartment), ct);
                await _imports.UpdateRowAsync(row.Id, "created", "[]", "[]", ct);
                created++;
            }
            catch (DomainException exception)
            {
                await _imports.UpdateRowAsync(row.Id, "conflict", JsonSerializer.Serialize(new[] { exception.Message }, Json), "[]", ct);
                await _imports.UpdateBatchAsync(batchId, "failed", null, 1, null, null, ct);
                throw new ConflictException($"Admin import stopped at row {row.RowNumber}: {exception.Message}");
            }
        }

        await _imports.UpdateBatchAsync(batchId, "published", null, 0, null, _current.DisplayName, ct);
        return new GovAdminImportPublishResult(created, 0);
    }

    public async Task DiscardAsync(Guid batchId, CancellationToken ct)
    {
        RequireSuperadmin();
        _ = await _imports.GetBatchAsync(batchId, ct) ?? throw new NotFoundException("Admin import batch not found.");
        await _imports.UpdateBatchAsync(batchId, "discarded", null, null, null, null, ct);
    }

    private static List<string> Validate(GovAdminImportRowRequest row, HashSet<string> seen)
    {
        var errors = new List<string>();
        var username = (row.Username ?? string.Empty).Trim();
        var displayName = (row.DisplayName ?? string.Empty).Trim();
        if (username.Length < 3) errors.Add("Username must contain at least 3 characters.");
        if (displayName.Length < 2) errors.Add("Display name is required.");
        if (!Enum.TryParse<GovAdminTier>(row.Tier, true, out var tier)) errors.Add("Tier is invalid.");
        if (!seen.Add(username)) errors.Add("Username is duplicated in this import.");

        if (tier == GovAdminTier.DistrictHead && (string.IsNullOrWhiteSpace(row.AssignedDistrict) || !KarnatakaAdministrativeUnits.IsValidDistrict(row.AssignedDistrict)))
            errors.Add("District Head requires a valid district.");
        if (tier == GovAdminTier.TalukHead && (string.IsNullOrWhiteSpace(row.AssignedDistrict) || string.IsNullOrWhiteSpace(row.AssignedTaluk) || !KarnatakaAdministrativeUnits.IsValidTaluk(row.AssignedDistrict, row.AssignedTaluk)))
            errors.Add("Taluk Head requires a valid district and taluk.");
        if (tier == GovAdminTier.Department && string.IsNullOrWhiteSpace(row.AssignedDepartment)) errors.Add("Department tier requires a department.");
        return errors;
    }

    private static GovAdminImportBatchDto ToDto(SevaOne.Domain.Models.GovAdminImportBatch batch) =>
        new(batch.Id, batch.SourceFilename, batch.SourceFormat, batch.UploadedBy, batch.UploadedAt,
            batch.Status, batch.RowCount, batch.ErrorCount, batch.WarningCount, batch.PublishedAt, batch.PublishedBy);

    private static GovAdminImportRowDto ToDto(SevaOne.Domain.Models.GovAdminImportRow row)
    {
        var errors = JsonSerializer.Deserialize<List<string>>(row.ValidationErrors, Json) ?? new();
        var warnings = JsonSerializer.Deserialize<List<string>>(row.ValidationWarnings, Json) ?? new();
        return new GovAdminImportRowDto(row.Id, row.RowNumber, row.Action, errors, warnings,
            JsonSerializer.Deserialize<GovAdminImportRowRequest>(row.RawPayload, Json));
    }

    private void RequireSuperadmin()
    {
        if (_current.Tier != GovAdminTier.Superadmin) throw new ForbiddenException("Only a Superadmin can manage admin imports.");
    }
}
