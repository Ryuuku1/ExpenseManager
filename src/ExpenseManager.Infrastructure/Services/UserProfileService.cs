using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Users.Models;
using ExpenseManager.Application.Users.Requests;
using ExpenseManager.Application.Users.Services;
using ExpenseManager.Domain.Entities.Users;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class UserProfileService : IUserProfileService
{
    private readonly ExpenseManagerDbContext _dbContext;

    public UserProfileService(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new UserProfile(
            user.Id,
            user.FullName,
            user.Email,
            user.MonthlyBudget,
            user.PreferredLanguage,
            user.PreferredCurrency);
    }

    public async Task UpdateProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        user.UpdateProfile(request.Name, request.PreferredLanguage);
        user.UpdateBudget(request.MonthlyBudget);
        user.UpdateCurrency(request.PreferredCurrency);

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.UpdateContact(request.Email);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateProfileAsync(string displayName, string email, string preferredLanguage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalizedLanguage = NormalizeLanguage(preferredLanguage);
        var user = User.Create(displayName.Trim(), email.Trim(), 0m, normalizedLanguage, Currency.Eur);
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    private static string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        try
        {
            return CultureInfo.GetCultureInfo(language).Name;
        }
        catch (CultureNotFoundException)
        {
            return "en";
        }
    }
}
