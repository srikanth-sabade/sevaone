namespace SevaOne.Services.Auth;

public sealed class CookieSettings
{
    public bool Secure { get; set; }
    public string SameSite { get; set; } = "Lax";
    public string Path { get; set; } = "/";
}
