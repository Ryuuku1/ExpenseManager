using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExpenseManager.Desktop.Services;

public interface IUserSessionService
{
    bool IsAuthenticated { get; }

    string? DisplayName { get; }

    string? UserName { get; }

    Guid? UserId { get; }

    string? PreferredLanguage { get; }

    Task<bool> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default);

    Task RegisterAsync(string userName, string password, string? displayName, CancellationToken cancellationToken = default);

    void UpdatePreferredLanguage(string? cultureName);

    void SignOut();
}
