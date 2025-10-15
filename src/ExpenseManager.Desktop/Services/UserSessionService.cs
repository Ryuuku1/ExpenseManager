using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Users.Services;

namespace ExpenseManager.Desktop.Services;

internal sealed class UserSessionService : IUserSessionService
{
    private readonly object _syncRoot = new();
    private readonly IAuthenticationStore _authenticationStore;
    private readonly IUserProfileService _userProfileService;
    private readonly IUserReadService _userReadService;
    private readonly ILocalizationService _localizationService;

    public bool IsAuthenticated { get; private set; }

    public string? DisplayName { get; private set; }

    public string? UserName { get; private set; }

    public Guid? UserId { get; private set; }

    public string? PreferredLanguage { get; private set; }

    public UserSessionService(
        IAuthenticationStore authenticationStore,
        IUserProfileService userProfileService,
        IUserReadService userReadService,
        ILocalizationService localizationService)
    {
        _authenticationStore = authenticationStore;
        _userProfileService = userProfileService;
        _userReadService = userReadService;
        _localizationService = localizationService;
    }

    public async Task<bool> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var credentials = await _authenticationStore.FindAsync(userName, cancellationToken);
        if (credentials is null || !credentials.Matches(userName, password))
        {
            return false;
        }

        var userId = await EnsureUserAsync(credentials, cancellationToken);
        var normalizedCredentials = credentials.UserId == userId
            ? credentials
            : credentials.WithUserId(userId);

        var appliedCulture = await ApplyUserCultureAsync(userId, cancellationToken);

        normalizedCredentials = normalizedCredentials
            .WithPreferredLanguage(appliedCulture)
            .WithLastAuthenticatedAtUtc(DateTime.UtcNow);

        await _authenticationStore.UpsertAsync(normalizedCredentials, cancellationToken);

        lock (_syncRoot)
        {
            IsAuthenticated = true;
            DisplayName = normalizedCredentials.DisplayName;
            UserName = normalizedCredentials.UserName;
            UserId = userId;
            PreferredLanguage = appliedCulture;
        }

        return true;
    }

    public async Task RegisterAsync(string userName, string password, string? displayName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(userName));       
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));      
        }

        if (await _authenticationStore.FindAsync(userName, cancellationToken) is not null)
        {
            throw new InvalidOperationException("User already exists.");
        }

        var credentials = AuthenticationCredentials.Create(userName, password, displayName);
        var userId = await CreateUserProfileAsync(credentials, cancellationToken);
        var normalizedCredentials = credentials.WithUserId(userId);
        var appliedCulture = await ApplyUserCultureAsync(userId, cancellationToken);

        normalizedCredentials = normalizedCredentials
            .WithPreferredLanguage(appliedCulture)
            .WithLastAuthenticatedAtUtc(DateTime.UtcNow);

        await _authenticationStore.UpsertAsync(normalizedCredentials, cancellationToken);

        lock (_syncRoot)
        {
            IsAuthenticated = true;
            DisplayName = normalizedCredentials.DisplayName;
            UserName = normalizedCredentials.UserName;
            UserId = userId;
            PreferredLanguage = appliedCulture;
        }
    }

    public void UpdatePreferredLanguage(string? cultureName)
    {
        string? normalized = null;

        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            try
            {
                normalized = CultureInfo.GetCultureInfo(cultureName).Name;
            }
            catch (CultureNotFoundException)
            {
                normalized = null;
            }
        }

        lock (_syncRoot)
        {
            PreferredLanguage = normalized;
        }
    }

    public void SignOut()
    {
        lock (_syncRoot)
        {
            IsAuthenticated = false;
            DisplayName = null;
            UserName = null;
            UserId = null;
            PreferredLanguage = null;
        }
    }

    private async Task<Guid> EnsureUserAsync(AuthenticationCredentials credentials, CancellationToken cancellationToken)
    {
        if (credentials.UserId != Guid.Empty)
        {
            var exists = await _userReadService.UserExistsAsync(credentials.UserId, cancellationToken);
            if (exists)
            {
                return credentials.UserId;
            }
        }

        var defaultUserId = await _userReadService.GetDefaultUserIdAsync(cancellationToken);
        if (defaultUserId is not null)
        {
            return defaultUserId.Value;
        }

        return await CreateUserProfileAsync(credentials, cancellationToken);
    }

    private async Task<Guid> CreateUserProfileAsync(AuthenticationCredentials credentials, CancellationToken cancellationToken)
    {
        var email = BuildEmail(credentials.UserName);
        var culture = CultureInfo.CurrentUICulture?.Name ?? "en";
        return await _userProfileService.CreateProfileAsync(credentials.DisplayName, email, culture, cancellationToken);
    }

    private async Task<string?> ApplyUserCultureAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _userProfileService.GetProfileAsync(userId, cancellationToken);
        if (profile is null || string.IsNullOrWhiteSpace(profile.PreferredLanguage))
        {
            return null;
        }

        var normalized = NormalizeCulture(profile.PreferredLanguage);
        _localizationService.TryApplyCulture(normalized, out _);
        return normalized;
    }

    private static string BuildEmail(string userName)
    {
        var normalized = userName.Replace(" ", string.Empty, StringComparison.Ordinal);
        return normalized.Contains('@', StringComparison.Ordinal)
            ? normalized
            : $"{normalized}@local";
    }

    private static string NormalizeCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return CultureInfo.CurrentUICulture.Name;
        }

        try
        {
            return CultureInfo.GetCultureInfo(cultureName).Name;
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentUICulture.Name;
        }
    }
}
