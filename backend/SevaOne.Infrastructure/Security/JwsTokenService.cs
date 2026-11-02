using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SevaOne.Infrastructure.Security;

public sealed class JwsTokenService : IJwsTokenService
{
    private readonly JwsOptions _options;

    public JwsTokenService(IOptions<JwsOptions> options)
    {
        _options = options.Value;
    }

    public (string TokenValue, string TokenHash) Mint(JwsClaims claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var subjectClaims = new[]
        {
            new Claim("sub", claims.SevaOneUserId.ToString()),
            new Claim("dept_id", claims.DepartmentId),
            new Claim("service_id", claims.ServiceId),
            new Claim("assisted_mode", claims.AssistedMode ? "true" : "false", ClaimValueTypes.Boolean),
            new Claim("operator_id", claims.OperatorId ?? string.Empty)
        };

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: claims.DepartmentId,
            claims: subjectClaims,
            notBefore: claims.IssuedAt.UtcDateTime,
            expires: claims.ExpireAt.UtcDateTime,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (tokenValue, ComputeHash(tokenValue));
    }

    public string ComputeHash(string tokenValue) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenValue))).ToLowerInvariant();

    public bool VerifySignature(string tokenValue)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = false,
            ValidateLifetime = false, // expiry is a per-caller business rule — see interface doc comment
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key
        };

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(tokenValue, parameters, out _);
            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
