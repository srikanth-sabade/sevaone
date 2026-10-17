namespace SevaOne.Infrastructure.Caching;

/// <summary>The operator's kiosk/counter cookie session, cached in Redis -- distinct from SevaSession (citizen) and from the sevaone_token JWS (department handoff). Separate cookie, separate auth scheme -- an operator and a citizen can be logged in on the same browser at once (assisted-mode counters).</summary>
public sealed class OperatorSession
{
    public string SessionId { get; set; } = string.Empty;
    public Guid OperatorId { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string? OperatorName { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>The citizen this operator has bound via OTP for assisted-mode apply (see
    /// OperatorCitizenBindService) -- null until a bind succeeds. Cleared on logout (the whole
    /// session is deleted) or explicit unbind. Never trust a client-supplied citizen id for an
    /// assisted apply; this is the only server-side source of truth for "who is the operator
    /// currently acting on behalf of".</summary>
    public Guid? ActiveCitizenId { get; set; }
}
