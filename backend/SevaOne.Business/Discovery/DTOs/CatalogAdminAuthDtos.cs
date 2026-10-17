namespace SevaOne.Business.Discovery.DTOs;

public sealed record CatalogAdminLoginRequest(string AdminCode, string Password);
public sealed record CatalogAdminLoginResult(string SessionId, string? AdminName, TimeSpan Ttl);
public sealed record CatalogAdminLogoutRequest(string? SessionId);
