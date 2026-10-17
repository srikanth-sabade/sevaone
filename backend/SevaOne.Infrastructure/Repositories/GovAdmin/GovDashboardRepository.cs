using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.GovAdmin;

public sealed class GovDashboardRepository : RepositoryBase, IGovDashboardRepository
{
    private readonly IDbConnectionFactory _factory;

    public GovDashboardRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<GovCitizenTotalsRow> GetCitizenTotalsAsync(string? district, string? taluk, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_citizen_totals(@district, @taluk)",
            new { district, taluk }, cancellationToken: ct);
        return await conn.QuerySingleAsync<GovCitizenTotalsRow>(cmd);
    }

    public async Task<IReadOnlyList<GovRegistrationTrendRow>> GetRegistrationTrendAsync(string? district, string? taluk, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_registration_trend(@district, @taluk)",
            new { district, taluk }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovRegistrationTrendRow>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<GovStatusCountRow>> GetApplicationStatusBreakdownAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_application_status_breakdown(@district, @taluk, @departmentCode)",
            new { district, taluk, departmentCode }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovStatusCountRow>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<GovApplySourceRow>> GetApplySourceSplitAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_apply_source_split(@district, @taluk, @departmentCode)",
            new { district, taluk, departmentCode }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovApplySourceRow>(cmd);
        return rows.AsList();
    }

    public async Task<GovDepartmentServiceCountsRow> GetDepartmentServiceCountsAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_department_service_counts()",
            cancellationToken: ct);
        return await conn.QuerySingleAsync<GovDepartmentServiceCountsRow>(cmd);
    }

    public async Task<IReadOnlyList<GovDepartmentBreakdownRow>> GetDepartmentBreakdownAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_department_breakdown()",
            cancellationToken: ct);
        var rows = await conn.QueryAsync<GovDepartmentBreakdownRow>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<GovDistrictRow>> GetDistrictRowsAsync(string? district, string? taluk, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_district_rows(@district, @taluk)",
            new { district, taluk }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovDistrictRow>(cmd);
        return rows.AsList();
    }

    public async Task<GovDiscoveryTotalsRow> GetDiscoveryTotalsAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_discovery_totals(@district, @taluk, @departmentCode)",
            new { district, taluk, departmentCode }, cancellationToken: ct);
        return await conn.QuerySingleAsync<GovDiscoveryTotalsRow>(cmd);
    }

    public async Task<IReadOnlyList<GovDiscoveryByDepartmentRow>> GetDiscoveryByDepartmentAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_discovery_by_department(@district, @taluk, @departmentCode)",
            new { district, taluk, departmentCode }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovDiscoveryByDepartmentRow>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<GovLifecycleTrendRow>> GetLifecycleTrendAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_lifecycle_trend(@district, @taluk, @departmentCode)",
            new { district, taluk, departmentCode }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovLifecycleTrendRow>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<GovSakalaSlaRow>> GetSakalaSlaAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_sakala_sla(@district, @taluk, @departmentCode)",
            new { district, taluk, departmentCode }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovSakalaSlaRow>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<GovApplicationAgingRow>> GetApplicationAgingAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_application_aging(@district, @taluk, @departmentCode)",
            new { district, taluk, departmentCode }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovApplicationAgingRow>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<GovSlaTrendRow>> GetSlaTrendAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_sla_trend(@district, @taluk, @departmentCode)",
            new { district, taluk, departmentCode }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovSlaTrendRow>(cmd);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<GovServiceUsageRow>> GetServiceUsageAsync(string? district, string? taluk, string? departmentCode, DateTimeOffset? since, DateTimeOffset? until, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition("SELECT * FROM portal.sp_get_gov_service_usage(@district, @taluk, @departmentCode, @since, @until)", new { district, taluk, departmentCode, since, until }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovServiceUsageRow>(cmd);
        return rows.AsList();
    }

    public async Task<GovApplicationFunnelRow> GetApplicationFunnelAsync(string? district, string? taluk, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<GovApplicationFunnelRow>(new CommandDefinition("SELECT * FROM portal.sp_get_gov_application_funnel(@district, @taluk)", new { district, taluk }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<GovApplicationDropoffRow>> GetApplicationDropoffAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<GovApplicationDropoffRow>(new CommandDefinition("SELECT * FROM portal.sp_get_gov_application_dropoff(@district, @taluk, @departmentCode)", new { district, taluk, departmentCode }, cancellationToken: ct));
        return rows.AsList();
    }
}
