using ProductApi.Infrastructure.Auth;
using Xunit;

namespace ProductApi.UnitTests;

public class DemoUserStoreTests
{
    private readonly DemoUserStore _sut = new();

    [Fact]
    public void Authenticate_returns_roles_for_valid_admin_credentials()
    {
        var roles = _sut.Authenticate("admin", "admin123");

        Assert.NotNull(roles);
        Assert.Contains("ADMIN", roles);
        Assert.Contains("USER", roles);
    }

    [Fact]
    public void Authenticate_returns_roles_for_valid_user_credentials()
    {
        var roles = _sut.Authenticate("user", "user123");

        Assert.NotNull(roles);
        Assert.DoesNotContain("ADMIN", roles);
        Assert.Contains("USER", roles);
    }

    [Fact]
    public void Authenticate_returns_null_for_wrong_password()
    {
        Assert.Null(_sut.Authenticate("admin", "wrong"));
    }

    [Fact]
    public void Authenticate_returns_null_for_unknown_username()
    {
        Assert.Null(_sut.Authenticate("nobody", "whatever"));
    }
}
