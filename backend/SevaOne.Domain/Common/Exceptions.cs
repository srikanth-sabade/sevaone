namespace SevaOne.Domain.Common;

/// <summary>
/// Code is optional and additive -- most throw sites (citizen-facing paths) still pass none,
/// and the global exception handler falls back to {title}-only for those, unchanged from
/// before. Only /api/v1/dept/* throw sites currently set it, using the catalog in
/// SevaErrorCodes -- see backend/docs/ERROR_CODES.md for why department-facing errors get a
/// stable machine code and citizen-facing ones (so far) don't.
/// </summary>
public class DomainException : Exception
{
    public string? Code { get; }

    public DomainException(string message, string? code = null) : base(message)
    {
        Code = code;
    }
}

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message, string? code = null) : base(message, code)
    {
    }
}

public sealed class ConflictException : DomainException
{
    public ConflictException(string message, string? code = null) : base(message, code)
    {
    }
}

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message, string? code = null) : base(message, code)
    {
    }
}

public sealed class DepartmentNotIntegratedException : DomainException
{
    public string DepartmentCode { get; }

    public DepartmentNotIntegratedException(string departmentCode)
        : base($"Department '{departmentCode}' is not yet integrated with SevaOne.")
    {
        DepartmentCode = departmentCode;
    }
}
