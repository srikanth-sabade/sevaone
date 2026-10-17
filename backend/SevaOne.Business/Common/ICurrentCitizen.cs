namespace SevaOne.Business.Common;

/// <summary>Populated per-request by Services-layer middleware from the seva:session cookie. Scoped, not singleton.</summary>
public interface ICurrentCitizen
{
    bool IsAuthenticated { get; }
    Guid? CitizenId { get; }
    string? SessionId { get; }
    string? IpAddress { get; }
}
