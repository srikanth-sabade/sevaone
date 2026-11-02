namespace SevaOne.Infrastructure.AiBackend;

/// <summary>
/// Plain Exception, deliberately NOT SevaOne.Domain.Common.DomainException -- Program.cs's
/// global exception middleware maps DomainException to a blanket 400, which is wrong here (an
/// unreachable/timed-out ai_backend is a gateway failure, not a bad request). Caught explicitly
/// in AiChatController instead and mapped to 502/504.
/// </summary>
public sealed class AiBackendUnavailableException : Exception
{
    public bool IsTimeout { get; }

    public AiBackendUnavailableException(string message, bool isTimeout, Exception? inner = null)
        : base(message, inner)
    {
        IsTimeout = isTimeout;
    }
}
