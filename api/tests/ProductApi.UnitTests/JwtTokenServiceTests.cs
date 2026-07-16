using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using ProductApi.Infrastructure.Auth;
using Xunit;

namespace ProductApi.UnitTests;

public class JwtTokenServiceTests
{
    private static RSA NewKey() => RSA.Create(2048);

    [Fact]
    public void IssueToken_returns_a_valid_jwt_with_expected_claims()
    {
        using var rsa = NewKey();
        var service = new JwtTokenService(rsa);

        var (token, expiresIn) = service.IssueToken("admin", ["ADMIN", "USER"]);

        Assert.Equal(3600, expiresIn);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("product-api", jwt.Issuer);
        Assert.Equal("admin", jwt.Subject);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "ADMIN");
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "USER");
    }

    [Fact]
    public void IssueToken_expires_in_one_hour()
    {
        using var rsa = NewKey();
        var service = new JwtTokenService(rsa);

        var (token, _) = service.IssueToken("user", ["USER"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var lifetime = jwt.ValidTo - jwt.ValidFrom;
        Assert.InRange(lifetime.TotalMinutes, 59, 61);
    }
}
