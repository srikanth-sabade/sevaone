namespace SevaOne.Infrastructure.Caching;

/// <summary>Login-handshake state only (PKCE/OTP-challenge staging) — citizens have no session object of their own to store/fetch/delete here anymore; see Wso2CitizenAuthMiddleware.</summary>
public interface ISevaSessionStore
{
    Task SaveLoginStateAsync(string state, LoginState payload, TimeSpan ttl, CancellationToken ct);

    /// <summary>Read-and-delete: a login state is consumed exactly once by the callback/OTP-verify step.</summary>
    Task<LoginState?> TakeLoginStateAsync(string state, CancellationToken ct);
}
