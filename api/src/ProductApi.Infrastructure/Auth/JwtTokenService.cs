using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace ProductApi.Infrastructure.Auth;

public sealed class JwtTokenService(RSA privateKey)
{
    public static readonly TimeSpan TokenLifespan = TimeSpan.FromHours(1);
    public const string Issuer = "product-api";

    public (string Token, long ExpiresInSeconds) IssueToken(string username, IReadOnlyCollection<string> roles)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, username) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(new RsaSecurityKey(privateKey), SecurityAlgorithms.RsaSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            claims: claims,
            notBefore: now,
            expires: now.Add(TokenLifespan),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), (long)TokenLifespan.TotalSeconds);
    }
}
