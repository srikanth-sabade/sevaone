using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Discovery;

public sealed class CatalogManagementRepository : RepositoryBase, ICatalogManagementRepository
{
    private readonly IDbConnectionFactory _factory;

    public CatalogManagementRepository(IDbConnectionFactory factory) : base(factory) => _factory = factory;

    public async Task<IReadOnlyList<CatalogAdminDepartmentRow>> ListDepartmentsAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CatalogAdminDepartmentRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_list_catalog_admin_departments()", cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<CatalogAdminDepartmentRow?> GetDepartmentAsync(string code, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CatalogAdminDepartmentRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_get_catalog_admin_department(@code)", new { code }, cancellationToken: ct));
    }

    public async Task<Guid> SaveDepartmentAsync(Guid id, string code, string name, string? nameKannada, string? url, string? urlKannada, int displayOrder, IUnitOfWork uow, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_admin_upsert_department(@id, @code, @name, @nameKannada, @url, @urlKannada, @displayOrder)",
            new { id, code, name, nameKannada, url, urlKannada, displayOrder }, uow.Transaction, cancellationToken: ct);
        return await uow.Connection.QuerySingleAsync<Guid>(cmd);
    }

    public async Task<bool> SetDepartmentActiveAsync(string code, bool isActive, IUnitOfWork uow, CancellationToken ct)
    {
        var cmd = new CommandDefinition("SELECT * FROM discovery.sp_admin_set_department_active(@code, @isActive)", new { code, isActive }, uow.Transaction, cancellationToken: ct);
        return await uow.Connection.QuerySingleAsync<bool>(cmd);
    }

    public async Task<IReadOnlyList<CatalogAdminSubDepartmentRow>> ListSubDepartmentsAsync(string? departmentCode, string? search, bool? isActive, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CatalogAdminSubDepartmentRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_list_sub_departments_for_admin(@departmentCode, @search, @isActive)",
            new { departmentCode, search, isActive }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<CatalogAdminSubDepartmentRow?> GetSubDepartmentAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CatalogAdminSubDepartmentRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_get_sub_department_for_admin(@id)", new { id }, cancellationToken: ct));
    }

    public async Task<Guid> SaveSubDepartmentAsync(Guid id, string departmentCode, string name, string? nameKannada, int displayOrder, bool isActive, IUnitOfWork uow, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_admin_upsert_sub_department(@id, @departmentCode, @name, @nameKannada, @displayOrder, @isActive)",
            new { id, departmentCode, name, nameKannada, displayOrder, isActive }, uow.Transaction, cancellationToken: ct);
        return await uow.Connection.QuerySingleAsync<Guid>(cmd);
    }

    public async Task<bool> SetSubDepartmentActiveAsync(Guid id, bool isActive, IUnitOfWork uow, CancellationToken ct)
    {
        var cmd = new CommandDefinition("SELECT * FROM discovery.sp_admin_set_sub_department_active(@id, @isActive)", new { id, isActive }, uow.Transaction, cancellationToken: ct);
        return await uow.Connection.QuerySingleAsync<bool>(cmd);
    }

    public async Task<IReadOnlyList<CatalogAdminServiceRow>> ListServicesAsync(string? departmentCode, string? search, bool? isActive, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CatalogAdminServiceRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_list_catalog_admin_services(@departmentCode, @search, @isActive)",
            new { departmentCode, search, isActive }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<CatalogAdminServiceRow?> GetServiceAsync(string departmentCode, string serviceCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CatalogAdminServiceRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_get_catalog_admin_service(@departmentCode, @serviceCode)",
            new { departmentCode, serviceCode }, cancellationToken: ct));
    }

    public async Task<Guid> SaveServiceAsync(CatalogAdminServiceWriteValues values, IUnitOfWork uow, CancellationToken ct)
    {
        var cmd = new CommandDefinition("""
            SELECT * FROM discovery.sp_admin_upsert_service(
                @Id, @DepartmentCode, @ServiceCode, @Name, @NameKannada, @SubDepartmentName,
                @ShortDescription, @ShortDescriptionKannada, @Description, @DescriptionKannada,
                @IsSakalaEnabled, @SakalaLabelRaw, @SakalaDays, @DepartmentFee, @ServiceCharge,
                @DocumentsJson, @DocumentsKannadaJson, @EligibilityJson, @EligibilityKannadaJson,
                @AliasesJson, @StepsJson, @StepsKannadaJson, @Jurisdiction, @Category,
                @ModeOfService, @DepartmentServiceId, @Portal, @ServiceUrl, @DeeplinkCitizenUrl,
                @DeeplinkCitizenUrlKannada, @DeeplinkOperatorUrl, @HomepageUrl, @IntegrationMethod,
                @SevasindhuServiceId, @Remarks, @IsActive)
            """, values, uow.Transaction, cancellationToken: ct);
        return await uow.Connection.QuerySingleAsync<Guid>(cmd);
    }

    public async Task<bool> SetServiceActiveAsync(string departmentCode, string serviceCode, bool isActive, IUnitOfWork uow, CancellationToken ct)
    {
        var cmd = new CommandDefinition("SELECT * FROM discovery.sp_admin_set_service_active(@departmentCode, @serviceCode, @isActive)", new { departmentCode, serviceCode, isActive }, uow.Transaction, cancellationToken: ct);
        return await uow.Connection.QuerySingleAsync<bool>(cmd);
    }

    public async Task<IReadOnlyList<CatalogAdminChangeRow>> ListChangesAsync(int limit, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CatalogAdminChangeRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_list_catalog_admin_changes(@limit)", new { limit }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<Guid> InsertChangeAsync(Guid? serviceId, string changedBy, string changeType, string? diff, IUnitOfWork uow, CancellationToken ct)
    {
        var cmd = new CommandDefinition("""
            SELECT * FROM discovery.sp_insert_service_change_log(
                @id, @serviceId, NULL, @changedBy, @changeType, @diff)
            """, new { id = Guid.NewGuid(), serviceId, changedBy, changeType, diff }, uow.Transaction, cancellationToken: ct);
        return await uow.Connection.QuerySingleAsync<Guid>(cmd);
    }
}
