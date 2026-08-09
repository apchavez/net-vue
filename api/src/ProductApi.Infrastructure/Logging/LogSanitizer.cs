namespace ProductApi.Infrastructure.Logging;

/// <summary>
/// Strips CR/LF and other control characters from untrusted input before it reaches a log
/// sink, preventing an attacker from forging fake log entries or injecting terminal escape
/// sequences via a crafted username, SKU, or request path.
/// </summary>
public static class LogSanitizer
{
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var chars = new char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            chars[i] = char.IsControl(value[i]) ? '_' : value[i];
        }
        return new string(chars);
    }
}
