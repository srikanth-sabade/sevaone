namespace SevaOne.Infrastructure.Repositories.Common;

// Plain settable-property class, not a positional record/value tuple -- required by Dapper's
// materializer for the same reason documented on IDiscoveryRepository (see DapperBootstrap).
// Shared shape every write-path sp_* function returns: RETURNS TABLE(success boolean, message text).
public sealed class WriteResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// portal.sp_insert_citizen is the one write function whose caller needs the generated id back
// (Citizen.SevaOneUserId, previously read via ExecuteScalarAsync<long> off a RETURNING clause).
public sealed class InsertCitizenResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long SevaOneUserId { get; set; }
}

// portal.sp_claim_operator also needs the generated id back (null when Success is false --
// operator_id was already claimed, see that function's unique_violation branch).
public sealed class ClaimOperatorResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? Id { get; set; }
}
