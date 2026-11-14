namespace ProductApi.Infrastructure.Auth;

/// <summary>
/// Hardcoded demo users for this portfolio project — not a real user store.
/// A production system would back this with a persisted, hashed credential store.
/// Mirrors the Quarkus sibling's DemoUserStore exactly (same demo credentials).
/// </summary>
public sealed class DemoUserStore
{
    private sealed record DemoUser(string PasswordHash, string[] Roles);

    private readonly Dictionary<string, DemoUser> _users = new()
    {
        ["admin"] = new DemoUser(BCrypt.Net.BCrypt.HashPassword("admin123"), ["ADMIN", "USER"]),
        ["user"] = new DemoUser(BCrypt.Net.BCrypt.HashPassword("user123"), ["USER"])
    };

    public string[]? Authenticate(string username, string password)
    {
        if (!_users.TryGetValue(username, out var user)) return null;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user.Roles : null;
    }
}
