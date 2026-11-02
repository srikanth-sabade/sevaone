namespace SevaOne.Business.Portal.DTOs;

public sealed record OperatorLoginRequest(string OperatorCode, string Password);
public sealed record OperatorLoginResult(string SessionId, string? OperatorName, TimeSpan Ttl);
public sealed record OperatorLogoutRequest(string? SessionId);
