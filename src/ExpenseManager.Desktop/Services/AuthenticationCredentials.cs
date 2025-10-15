using System;
using System.Globalization;

namespace ExpenseManager.Desktop.Services;

public sealed record AuthenticationCredentials(
    Guid UserId,
    string UserName,
    string Password,
    string DisplayName,
    string? PreferredLanguage = null,
    DateTime? LastAuthenticatedAtUtc = null)
{
    public static AuthenticationCredentials Create(string userName, string password, string? displayName, Guid? userId = null, string? preferredLanguage = null, DateTime? lastAuthenticatedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(userName));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        var normalizedUserName = userName.Trim();
        var normalizedDisplayName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName.Trim()
            : BuildDisplayName(normalizedUserName);

        var normalizedLanguage = NormalizeLanguage(preferredLanguage);

        return new AuthenticationCredentials(
            userId ?? Guid.Empty,
            normalizedUserName,
            password,
            normalizedDisplayName,
            normalizedLanguage,
            lastAuthenticatedAtUtc);
    }

    public AuthenticationCredentials WithUserId(Guid userId) => this with { UserId = userId };

    public AuthenticationCredentials WithPreferredLanguage(string? preferredLanguage) => this with { PreferredLanguage = NormalizeLanguage(preferredLanguage) };

    public AuthenticationCredentials WithLastAuthenticatedAtUtc(DateTime? authenticatedAtUtc) => this with { LastAuthenticatedAtUtc = authenticatedAtUtc };

    public bool Matches(string? userName, string? password)
    {
        if (userName is null || password is null)
        {
            return false;
        }

        return string.Equals(UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase)
               && string.Equals(Password, password, StringComparison.Ordinal);
    }

    private static string BuildDisplayName(string userName)
    {
        var trimmed = userName.Trim();
        return char.ToUpperInvariant(trimmed[0]) + (trimmed.Length > 1 ? trimmed[1..] : string.Empty);
    }

    private static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        try
        {
            var trimmed = language.Trim();
            return trimmed.Length == 0
                ? null
                : CultureInfo.GetCultureInfo(trimmed).Name;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
