using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Portal;

public sealed class PortalRepository : RepositoryBase, IPortalRepository
{
    private readonly IDbConnectionFactory _factory;

    public PortalRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<Citizen?> GetCitizenByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_citizen_by_id(@id)",
            new { id }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Citizen>(cmd);
    }

    public async Task<Citizen?> GetCitizenByWso2SubjectAsync(string wso2Subject, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_citizen_by_wso2_subject(@wso2Subject)",
            new { wso2Subject }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Citizen>(cmd);
    }

    public async Task<Citizen?> GetCitizenByUsernameAsync(string username, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_citizen_by_username(@username)",
            new { username }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Citizen>(cmd);
    }

    public async Task<Citizen?> GetCitizenBySevaOneUserIdAsync(long sevaOneUserId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_citizen_by_sevaone_user_id(@sevaOneUserId)",
            new { sevaOneUserId }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Citizen>(cmd);
    }

    public async Task<Citizen?> GetCitizenByMobileAsync(string mobile, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_citizen_by_mobile(@mobile)",
            new { mobile }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Citizen>(cmd);
    }

    public async Task<Citizen?> GetCitizenByAadhaarHashAsync(string aadhaarHash, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_citizen_by_aadhaar_hash(@aadhaarHash)",
            new { aadhaarHash }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Citizen>(cmd);
    }

    public async Task<Citizen> InsertCitizenAsync(Citizen citizen, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM portal.sp_insert_citizen(
                @Id, @Wso2Subject, @Username, @Name, @Mobile, @MobileVerifiedAt,
                @Email, @Address, @District, @Taluk, @StateName, @VillageName, @DateOfBirth, @Gender,
                @AadhaarMasked, @AadhaarHash, @DigiLockerId, @Photo, @NameKannada, @AddressKannada, @DistrictKannada,
                @StateKannada, @VillageKannada, @SevaId, @ProfileCompleted, @ProfileFinishPct,
                @CreatedByOperatorId, @CreatedAt, @UpdatedAt)
            """;
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(sql, citizen, cancellationToken: ct);
        var result = await conn.QuerySingleAsync<InsertCitizenResult>(cmd);
        citizen.SevaOneUserId = result.SevaOneUserId;
        return citizen;
    }

    public async Task UpdateCitizenAsync(Citizen citizen, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM portal.sp_update_citizen(
                @Id, @Name, @Mobile, @MobileVerifiedAt, @Email, @Address, @District,
                @Taluk, @StateName, @VillageName, @DateOfBirth, @Gender, @AadhaarMasked, @AadhaarHash,
                @DigiLockerId, @Photo, @NameKannada, @AddressKannada, @DistrictKannada, @StateKannada, @VillageKannada,
                @ProfileCompleted, @ProfileFinishPct, @UpdatedAt, @Occupation)
            """;
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(sql, citizen, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
    }

    public async Task DeleteCitizenAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition("SELECT * FROM portal.sp_delete_citizen(@id)", new { id }, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
    }

    public async Task<OperatorMaster?> GetOperatorByCodeAsync(string operatorCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_operator_by_code(@operatorCode)",
            new { operatorCode }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<OperatorMaster>(cmd);
    }

    public async Task<OperatorMaster?> GetOperatorByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_operator_by_id(@id)",
            new { id }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<OperatorMaster>(cmd);
    }

    public async Task<OperatorCredentials?> GetOperatorCredentialsByCodeAsync(string operatorCode, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_operator_credentials(@operatorCode)",
            new { operatorCode }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<OperatorCredentials>(cmd);
    }

    public async Task<GovAdminCredentials?> GetGovAdminByUsernameAsync(string username, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_admin_by_username(@username)",
            new { username }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<GovAdminCredentials>(cmd);
    }

    public async Task<(bool Success, string Message, Guid? Id)> CreateGovAdminAsync(
        Guid id, string username, string displayName, string tier, string? assignedDistrict,
        string? assignedTaluk, string? assignedDepartment, DateTimeOffset createdAt, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_create_gov_admin(@id, @username, @displayName, @tier, @assignedDistrict, @assignedTaluk, @assignedDepartment, @createdAt)",
            new { id, username, displayName, tier, assignedDistrict, assignedTaluk, assignedDepartment, createdAt }, cancellationToken: ct);
        var result = await conn.QuerySingleAsync<ClaimOperatorResult>(cmd);
        return (result.Success, result.Message, result.Id);
    }

    public async Task<IReadOnlyList<GovAdminSummary>> ListGovAdminsAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition("SELECT * FROM portal.sp_list_gov_admins()", cancellationToken: ct);
        var rows = await conn.QueryAsync<GovAdminSummary>(cmd);
        return rows.AsList();
    }

    public async Task<bool> SetGovAdminActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_set_gov_admin_active(@id, @isActive)",
            new { id, isActive }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<bool?>(cmd) ?? false;
    }

    public async Task<IReadOnlyList<OperatorAdminSummary>> ListOperatorsForAdminAsync(string? search, bool? isActive, string? operatorType, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_list_operators_for_admin(@search, @isActive, @operatorType)",
            new { search, isActive, operatorType }, cancellationToken: ct);
        var rows = await conn.QueryAsync<OperatorAdminSummary>(cmd);
        return rows.AsList();
    }

    public async Task<bool> SetOperatorActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_set_operator_active(@id, @isActive)",
            new { id, isActive }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<bool?>(cmd) ?? false;
    }

    public async Task<(bool Success, string Message)> ClaimGovAdminAsync(
        string username, string wso2Subject, string mobileNo, DateTimeOffset mobileVerifiedAt, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_claim_gov_admin(@username, @wso2Subject, @mobileNo, @mobileVerifiedAt)",
            new { username, wso2Subject, mobileNo, mobileVerifiedAt }, cancellationToken: ct);
        var result = await conn.QuerySingleAsync<WriteResult>(cmd);
        return (result.Success, result.Message);
    }

    public async Task<OperatorWhitelistEntry?> GetOperatorWhitelistEntryAsync(long userId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_operator_whitelist_entry(@userId)",
            new { userId }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<OperatorWhitelistEntry>(cmd);
    }

    public async Task<(bool Success, string Message, Guid? Id)> ClaimOperatorAsync(
        Guid id, string operatorId, string mobileNo, DateTimeOffset mobileVerifiedAt, string wso2Subject, DateTimeOffset createdAt, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_claim_operator(@id, @operatorId, 'UNK', @mobileNo, @mobileVerifiedAt, @wso2Subject, @createdAt)",
            new { id, operatorId, mobileNo, mobileVerifiedAt, wso2Subject, createdAt }, cancellationToken: ct);
        var result = await conn.QuerySingleAsync<ClaimOperatorResult>(cmd);
        return (result.Success, result.Message, result.Id);
    }

    public async Task InsertTokenAsync(SevaOneToken token, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM portal.sp_insert_token(
                @Id, @CitizenId, @SevaOneUserId, @OperatorId, @AssistedMode, @DepartmentId, @SchemeId,
                @TokenValue, @TokenHash, @IssuedAt, @ExpireAt, @MidnightValidUntil, @RevokedAt, @IsActive,
                @ReturnUrl)
            """;
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, token, tx, cancellationToken: ct);
            await conn.QuerySingleAsync<WriteResult>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<SevaOneToken?> GetTokenByHashAsync(string tokenHash, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_token_by_hash(@tokenHash)",
            new { tokenHash }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<SevaOneToken>(cmd);
    }

    public async Task<SevaOneToken?> GetTokenByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_token_by_id(@id)",
            new { id }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<SevaOneToken>(cmd);
    }

    public async Task<bool> TryRedeemTokenAsync(Guid tokenId, DateTimeOffset redeemedAt, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_try_redeem_token(@tokenId, @redeemedAt)",
            new { tokenId, redeemedAt }, cancellationToken: ct);
        var result = await conn.QuerySingleAsync<WriteResult>(cmd);
        return result.Success;
    }
}
