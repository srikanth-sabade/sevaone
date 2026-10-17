using SevaOne.Infrastructure.Connectors;

namespace SevaOne.Business.Orchestration.DTOs;

public sealed record OpenDepartmentSessionRequest(string DepartmentCode);

public sealed record DepartmentSessionDto(string DepartmentCode, DateTimeOffset ExpiresAt, bool Reused);

public sealed record OpenedDepartmentSession(DepartmentSessionHandle Handle, bool Reused);
