using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Discovery;

public sealed class DiscoveryRepository : RepositoryBase, IDiscoveryRepository
{
    private readonly IDbConnectionFactory _factory;

    public DiscoveryRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Department>> ListDepartmentsAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition("SELECT * FROM discovery.sp_list_departments()", cancellationToken: ct);
        var rows = await conn.QueryAsync<Department>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<DepartmentWithSchemeCount>> ListDepartmentsWithSchemeCountAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition("SELECT * FROM discovery.sp_list_departments_with_scheme_count()", cancellationToken: ct);
        var rows = await conn.QueryAsync<DepartmentWithSchemeCount>(cmd);
        return rows.AsList();
    }

    public async Task<Department?> GetDepartmentByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_department_by_id(@id)",
            new { id }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Department>(cmd);
    }

    public async Task<Department?> GetDepartmentByCodeAsync(string code, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_department_by_code(@code)",
            new { code }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Department>(cmd);
    }

    public async Task<IReadOnlyList<Scheme>> ListSchemesAsync(Guid? departmentId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_schemes(@departmentId)",
            new { departmentId }, cancellationToken: ct);
        var rows = await conn.QueryAsync<Scheme>(cmd);
        return rows.AsList();
    }

    public async Task<Scheme?> GetSchemeByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_scheme_by_id(@id)",
            new { id }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Scheme>(cmd);
    }

    public async Task<Scheme?> GetSchemeByCodeAsync(Guid departmentId, string serviceCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_scheme_by_code(@departmentId, @serviceCode)",
            new { departmentId, serviceCode }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Scheme>(cmd);
    }

    public async Task<Scheme?> GetSchemeByDepartmentAndServiceCodeAsync(string departmentCode, string serviceCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_scheme_by_department_and_service_code(@departmentCode, @serviceCode)",
            new { departmentCode, serviceCode }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Scheme>(cmd);
    }

    public async Task<ActiveSchemeByName?> FindActiveSchemeByNameAsync(string serviceName, string? departmentCode, string? operatorHint, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_find_active_scheme_by_name(@serviceName, @departmentCode, @operatorHint)",
            new { serviceName, departmentCode, operatorHint }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ActiveSchemeByName>(cmd);
    }

    public async Task<IReadOnlyList<SchemeWithDepartment>> ListSchemesWithDepartmentAsync(string? departmentCode, string? category, bool onlyActive, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_schemes_with_department(@departmentCode, @category, @onlyActive)",
            new { departmentCode, category, onlyActive }, cancellationToken: ct);
        var rows = await conn.QueryAsync<SchemeWithDepartment>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<SchemeWithDepartment>> ListTopRequestedServicesAsync(DateTimeOffset since, int limit, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_top_requested_services(@since, @limit)",
            new { since, limit }, cancellationToken: ct);
        var rows = await conn.QueryAsync<SchemeWithDepartment>(cmd);
        return rows.AsList();
    }

    public async Task<SchemeWithDepartment?> GetSchemeWithDepartmentByServiceCodeAsync(string serviceCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_scheme_with_department_by_service_code(@serviceCode)",
            new { serviceCode }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<SchemeWithDepartment>(cmd);
    }

    public async Task<IReadOnlyList<SevaOneServiceDocument>> GetDocumentsBySchemeAsync(Guid schemeId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_documents_by_scheme(@schemeId)",
            new { schemeId }, cancellationToken: ct);
        var rows = await conn.QueryAsync<SevaOneServiceDocument>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<SubDepartment>> ListSubDepartmentsByDepartmentAsync(Guid departmentId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_sub_departments_by_department(@departmentId)",
            new { departmentId }, cancellationToken: ct);
        var rows = await conn.QueryAsync<SubDepartment>(cmd);
        return rows.AsList();
    }

    public async Task<CatalogAdmin?> GetCatalogAdminByCodeAsync(string adminCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_catalog_admin_by_code(@adminCode)",
            new { adminCode }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<CatalogAdmin>(cmd);
    }

    // ---- Service Catalogue: write path ----

    public async Task<Guid> UpsertDepartmentAsync(Guid id, string code, string name, string? departmentUrl, string? departmentUrlKannada, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = "SELECT * FROM discovery.sp_upsert_department(@id, @code, @name, @departmentUrl, @departmentUrlKannada)";
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, new { id, code, name, departmentUrl, departmentUrlKannada }, tx, cancellationToken: ct);
            return await conn.QuerySingleAsync<Guid>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<Guid> UpsertSubDepartmentAsync(Guid id, Guid departmentId, string name, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = "SELECT * FROM discovery.sp_upsert_sub_department(@id, @departmentId, @name)";
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, new { id, departmentId, name }, tx, cancellationToken: ct);
            return await conn.QuerySingleAsync<Guid>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<Guid> UpsertServiceAsync(ServiceUpsertRequest request, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM discovery.sp_upsert_sevaone_service(
                @Id, @ServiceCode, @DepartmentId, @SubDepartmentId, @Name, @NameKannada,
                @Category, @SakalaDays, @IsSakalaEnabled, @SakalaLabelRaw, @Description, @DescriptionKannada, @ShortDescription,
                @DepartmentFee, @ServiceCharge, @DocumentsJson, @DocumentsJsonKannada,
                @EligibilityJson, @EligibilityJsonKannada,
                @StepsJson, @StepsJsonKannada, @Jurisdiction, @ModeOfService,
                @DepartmentServiceId, @ServiceUrl, @DeeplinkCitizenUrl, @DeeplinkCitizenUrlKannada)
            """;
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, request, tx, cancellationToken: ct);
            return await conn.QuerySingleAsync<Guid>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<Guid> UpsertServiceDocumentAsync(Guid id, Guid schemeId, string? documentName, string? documentNameKannada, string documentUrl, string? documentType, bool isMandatory, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM discovery.sp_upsert_service_document(
                @id, @schemeId, @documentName, @documentNameKannada, @documentUrl, @documentType, @isMandatory)
            """;
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, new { id, schemeId, documentName, documentNameKannada, documentUrl, documentType, isMandatory }, tx, cancellationToken: ct);
            return await conn.QuerySingleAsync<Guid>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    // ---- Service Catalogue: role-aware reads ----

    public async Task<IReadOnlyList<DepartmentForRole>> ListDepartmentsForRoleAsync(string roleKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_departments_for_role(@roleKey)",
            new { roleKey }, cancellationToken: ct);
        var rows = await conn.QueryAsync<DepartmentForRole>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<SubDepartmentForRole>> ListSubDepartmentsForRoleAsync(Guid departmentId, string roleKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_sub_departments_for_role(@departmentId, @roleKey)",
            new { departmentId, roleKey }, cancellationToken: ct);
        var rows = await conn.QueryAsync<SubDepartmentForRole>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ServiceForRole>> ListServicesByDepartmentForRoleAsync(Guid departmentId, string roleKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_services_by_department_for_role(@departmentId, @roleKey)",
            new { departmentId, roleKey }, cancellationToken: ct);
        var rows = await conn.QueryAsync<ServiceForRole>(cmd);
        return rows.AsList();
    }

    public async Task<ServiceDetailForRole?> GetServiceForRoleAsync(string departmentCode, string serviceCode, string roleKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_service_for_role(@departmentCode, @serviceCode, @roleKey)",
            new { departmentCode, serviceCode, roleKey }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ServiceDetailForRole>(cmd);
    }

    public async Task<IReadOnlyList<ServiceForAiExport>> ListServicesForAiExportAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition("SELECT * FROM discovery.sp_list_services_for_ai_export()", cancellationToken: ct);
        var rows = await conn.QueryAsync<ServiceForAiExport>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<DepartmentForRole>> SearchDepartmentsAsync(string query, string roleKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_search_departments(@query, @roleKey)",
            new { query, roleKey }, cancellationToken: ct);
        var rows = await conn.QueryAsync<DepartmentForRole>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ServiceSearchResultRow>> SearchServicesForRoleAsync(string query, string roleKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_search_services_for_role(@query, @roleKey)",
            new { query, roleKey }, cancellationToken: ct);
        var rows = await conn.QueryAsync<ServiceSearchResultRow>(cmd);
        return rows.AsList();
    }

    public async Task InsertServiceApplyTrackAsync(ServiceApplyTrack track, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM discovery.sp_insert_service_apply_track(
                @Id, @SchemeId, @DepartmentId, @CitizenId, @AssistedMode, @IsHomepageFallback, @CreatedAt, @Source)
            """;
        using var conn = _factory.CreateConnection();
        await conn.QuerySingleAsync<WriteResult>(new CommandDefinition(sql, track, cancellationToken: ct));
    }

    public async Task<long> GetServiceApplyCountAsync(Guid schemeId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT discovery.sp_get_service_apply_count(@schemeId)",
            new { schemeId }, cancellationToken: ct);
        return await conn.ExecuteScalarAsync<long>(cmd);
    }
}
