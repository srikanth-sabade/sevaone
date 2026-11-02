namespace SevaOne.Business.Common;

/// <summary>Populated per-request by Services-layer middleware from the seva.op.sid cookie -- the operator-session counterpart of ICurrentCitizen. Scoped, not singleton.</summary>
public interface ICurrentOperator
{
    bool IsAuthenticated { get; }
    Guid? OperatorId { get; }
    string? OperatorCode { get; }
    string? OperatorName { get; }
    string? SessionId { get; }
    string? IpAddress { get; }

    /// <summary>The citizen currently bound to this operator's session (see OperatorSession.ActiveCitizenId), if any. Read from the same OperatorSession the middleware already loaded for this request -- no extra Redis round trip.</summary>
    Guid? ActiveCitizenId { get; }
}
