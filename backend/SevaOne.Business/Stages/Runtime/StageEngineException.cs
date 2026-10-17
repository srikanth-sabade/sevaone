namespace SevaOne.Business.Stages.Runtime;

public enum EngineErrorCode
{
    /// <summary>The package supplied is not the exact version the case is pinned to.</summary>
    PackageMismatch,
    /// <summary>The action is not allowed in the case's or stage's current state.</summary>
    InvalidState,
    /// <summary>The officer does not hold the role this stage's approval requires.</summary>
    NotAuthorized,
    /// <summary>The fee reference does not belong to this case.</summary>
    UnknownFeeReference,
    /// <summary>Returning or rejecting needs a reason the applicant can read.</summary>
    NoteRequired,
}

public sealed class StageEngineException : Exception
{
    public StageEngineException(EngineErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public EngineErrorCode Code { get; }
}
