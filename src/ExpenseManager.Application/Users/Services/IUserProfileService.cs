using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Users.Models;
using ExpenseManager.Application.Users.Requests;

namespace ExpenseManager.Application.Users.Services;

public interface IUserProfileService
{
    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task UpdateProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

    Task<Guid> CreateProfileAsync(string displayName, string email, string preferredLanguage, CancellationToken cancellationToken = default);
}
