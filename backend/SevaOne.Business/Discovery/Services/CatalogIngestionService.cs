using System.Text.Json;
using System.Text.RegularExpressions;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Services;

public sealed partial class CatalogIngestionService : ICatalogIngestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ICatalogIngestionRepository _ingestion;
    private readonly IDiscoveryRepository _discovery;

    public CatalogIngestionService(ICatalogIngestionRepository ingestion, IDiscoveryRepository discovery)
    {
        _ingestion = ingestion;
        _discovery = discovery;
    }

    public async Task<Guid> StartImportAsync(string sourceFilename, string sourceFormat, string uploadedBy, IReadOnlyList<CatalogRow> rows, CancellationToken ct)
    {
        var batchId = Guid.NewGuid();
        await _ingestion.InsertImportBatchAsync(batchId, sourceFilename, sourceFormat, uploadedBy, ct);

        var seenComposite = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Existence checks are memoized per (departmentCode, serviceCode) within the batch so a
        // spreadsheet with 754 rows across 10 departments doesn't repeat a department lookup 754
        // times over -- still one DB round trip per distinct department/service pair, which is
        // fine for an admin batch operation, not a hot path.
        var departmentCache = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);
        int errorCount = 0, warningCount = 0;

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var raw = rows[i];
            var normalized = Normalize(raw);

            var rowId = Guid.NewGuid();
            await _ingestion.InsertImportRowAsync(
                rowId, batchId, rowNumber,
                JsonSerializer.Serialize(raw, JsonOptions),
                JsonSerializer.Serialize(normalized, JsonOptions),
                ct);

            var errors = new List<ValidationIssue>();
            var warnings = new List<ValidationIssue>();

            if (string.IsNullOrEmpty(normalized.Department.Code)) errors.Add(new ValidationIssue("department.code", "Department code is required."));
            if (string.IsNullOrEmpty(normalized.Department.Name)) errors.Add(new ValidationIssue("department.name", "Department name is required."));
            if (string.IsNullOrEmpty(normalized.Service.ServiceCode)) errors.Add(new ValidationIssue("service.serviceCode", "Service code is required."));
            if (string.IsNullOrEmpty(normalized.Service.Name)) errors.Add(new ValidationIssue("service.name", "Service name is required."));

            string action;
            if (errors.Count > 0)
            {
                // Can't classify insert/update/conflict against an incomplete identity -- the
                // row is blocked on the missing fields regardless.
                action = "conflict";
            }
            else
            {
                var compositeKey = $"{normalized.Department.Code}|{normalized.Service.ServiceCode}";
                if (!seenComposite.Add(compositeKey))
                {
                    errors.Add(new ValidationIssue("service.serviceCode", $"Duplicate composite code within this batch: {compositeKey}"));
                    action = "conflict";
                }
                else
                {
                    if (!departmentCache.TryGetValue(normalized.Department.Code, out var departmentId))
                    {
                        var dept = await _discovery.GetDepartmentByCodeAsync(normalized.Department.Code, ct);
                        departmentId = dept?.Id;
                        departmentCache[normalized.Department.Code] = departmentId;
                    }

                    if (departmentId is null)
                    {
                        action = "insert";
                    }
                    else
                    {
                        var scheme = await _discovery.GetSchemeByCodeAsync(departmentId.Value, normalized.Service.ServiceCode, ct);
                        action = scheme is null ? "insert" : "update";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(raw.Service.ModeOfService) && normalized.Service.ModeOfService is null)
            {
                warnings.Add(new ValidationIssue("service.modeOfService", $"Could not normalize \"{raw.Service.ModeOfService}\" to Online/Offline/Both -- left blank."));
            }

            await _ingestion.UpdateImportRowValidationAsync(
                rowId, action,
                JsonSerializer.Serialize(errors, JsonOptions),
                JsonSerializer.Serialize(warnings, JsonOptions),
                ct);

            errorCount += errors.Count > 0 ? 1 : 0;
            warningCount += warnings.Count > 0 ? 1 : 0;
        }

        var finalStatus = errorCount > 0 ? "failed" : "ready_to_publish";
        await _ingestion.UpdateImportBatchStatusAsync(batchId, finalStatus, rows.Count, errorCount, warningCount, null, ct);
        return batchId;
    }

    public async Task<ImportBatchStatusDto?> GetBatchAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await _ingestion.GetImportBatchAsync(batchId, ct);
        if (batch is null) return null;
        return new ImportBatchStatusDto(batch.Id, batch.SourceFilename, batch.SourceFormat, batch.UploadedBy, batch.UploadedAt,
            batch.Status, batch.RowCount, batch.ErrorCount, batch.WarningCount, batch.PublishedAt, batch.PublishedBy);
    }

    public async Task<IReadOnlyList<ImportRowDto>> ListRowsAsync(Guid batchId, string? action, CancellationToken ct)
    {
        var rows = await _ingestion.ListImportRowsAsync(batchId, action, ct);
        var result = new List<ImportRowDto>(rows.Count);
        foreach (var row in rows)
        {
            var errors = JsonSerializer.Deserialize<List<ValidationIssue>>(row.ValidationErrors, JsonOptions) ?? [];
            var warnings = JsonSerializer.Deserialize<List<ValidationIssue>>(row.ValidationWarnings, JsonOptions) ?? [];
            var normalized = string.IsNullOrEmpty(row.NormalizedPayload) ? null : JsonSerializer.Deserialize<CatalogRow>(row.NormalizedPayload, JsonOptions);
            result.Add(new ImportRowDto(row.Id, row.RowNumber, row.Action, errors, warnings,
                normalized?.Department.Code ?? "", normalized?.Service.ServiceCode ?? "", normalized?.Service.Name ?? ""));
        }
        return result;
    }

    public async Task DiscardAsync(Guid batchId, CancellationToken ct)
    {
        _ = await _ingestion.GetImportBatchAsync(batchId, ct) ?? throw new NotFoundException($"Import batch '{batchId}' was not found.");
        await _ingestion.UpdateImportBatchStatusAsync(batchId, "discarded", null, null, null, null, ct);
    }

    // ---- normalization ----

    private static CatalogRow Normalize(CatalogRow source) => new()
    {
        Department = new CatalogRowDepartment
        {
            Code = source.Department.Code?.Trim() ?? "",
            Name = source.Department.Name?.Trim() ?? "",
            HomeUrlEn = NullIfBlank(source.Department.HomeUrlEn),
        },
        SubDepartment = NormalizeSubDepartment(source.SubDepartment),
        Service = new CatalogRowService
        {
            ServiceCode = source.Service.ServiceCode?.Trim() ?? "",
            Name = source.Service.Name?.Trim() ?? "",
            NameKannada = NullIfBlank(source.Service.NameKannada),
            ShortDescription = NullIfBlank(source.Service.ShortDescription),
            Description = NullIfBlank(StripHtml(source.Service.Description)),
            DescriptionKannada = NullIfBlank(StripHtml(source.Service.DescriptionKannada)),
            IsSakalaEnabled = source.Service.IsSakalaEnabled,
            SakalaLabelRaw = NullIfBlank(source.Service.SakalaLabelRaw),
            SakalaDays = source.Service.SakalaDays,
            DepartmentFee = source.Service.DepartmentFee,
            ServiceCharge = source.Service.ServiceCharge,
            DocumentsEn = NormalizeList(source.Service.DocumentsEn),
            DocumentsKn = NormalizeList(source.Service.DocumentsKn),
            EligibilityEn = NormalizeList(source.Service.EligibilityEn),
            EligibilityKn = NormalizeList(source.Service.EligibilityKn),
            ProcedureEn = NormalizeList(source.Service.ProcedureEn),
            ProcedureKn = NormalizeList(source.Service.ProcedureKn),
            Jurisdiction = NullIfBlank(source.Service.Jurisdiction),
            Category = NullIfBlank(source.Service.Category),
            ModeOfService = NormalizeModeOfService(source.Service.ModeOfService),
            IsMandatory = source.Service.IsMandatory,
            DepartmentServiceId = NullIfBlank(source.Service.DepartmentServiceId),
            ServiceUrl = NullIfBlank(source.Service.ServiceUrl),
            ApplyUrl = NullIfBlank(source.Service.ApplyUrl),
        },
    };

    private static CatalogRowSubDepartment? NormalizeSubDepartment(CatalogRowSubDepartment? sub)
    {
        var name = sub?.Name?.Trim();
        // Blank or "-" means "no real sub-department" -- see catalogHierarchy.generated.ts's
        // own normalization notes from this same source data. No synthetic fallback row: the
        // service's SubDepartmentId is simply left null.
        if (string.IsNullOrEmpty(name) || name == "-") return null;
        return new CatalogRowSubDepartment { Name = name };
    }

    private static string? NormalizeModeOfService(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var lower = raw.ToLowerInvariant();
        var hasOnline = lower.Contains("online");
        var hasOffline = lower.Contains("offline");
        if (hasOnline && hasOffline) return "Both";
        if (hasOnline) return "Online";
        if (hasOffline) return "Offline";
        // Unrecognized -- left null rather than guessed; StartImportAsync adds a warning for this.
        return null;
    }

    private static List<string> NormalizeList(List<string>? items) =>
        items is null ? [] : items.Select(s => StripHtml(s).Trim()).Where(s => s.Length > 0).ToList();

    private static string StripHtml(string? text) =>
        string.IsNullOrEmpty(text) ? "" : HtmlTagRegex().Replace(text, " ").Trim();

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
