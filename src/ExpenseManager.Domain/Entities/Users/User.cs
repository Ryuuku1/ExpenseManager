using System;
using ExpenseManager.Domain.Abstractions;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Domain.Entities.Users;

public sealed class User : AggregateRoot
{
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PreferredLanguage { get; private set; }
    public decimal MonthlyBudget { get; private set; }
    public Currency PreferredCurrency { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

#pragma warning disable IDE0051
    private User() : this(Guid.NewGuid(), string.Empty, string.Empty, "en", 0, Currency.Eur)
    {
    }
#pragma warning restore IDE0051

    private User(Guid id, string fullName, string email, string preferredLanguage, decimal monthlyBudget, Currency preferredCurrency) : base(id)
    {
        FullName = fullName;
        Email = email;
        PreferredLanguage = preferredLanguage;
        MonthlyBudget = monthlyBudget;
        PreferredCurrency = preferredCurrency;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static User Create(string fullName, string email, decimal monthlyBudget, string preferredLanguage = "en", Currency preferredCurrency = Currency.Eur)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name cannot be empty.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        if (monthlyBudget < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyBudget), monthlyBudget, "Monthly budget cannot be negative.");
        }

        return new User(Guid.NewGuid(), fullName.Trim(), email.Trim(), preferredLanguage.Trim().ToLowerInvariant(), monthlyBudget, preferredCurrency);
    }

    public void UpdateProfile(string fullName, string preferredLanguage)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name cannot be empty.", nameof(fullName));
        }

        FullName = fullName.Trim();
        PreferredLanguage = preferredLanguage.Trim().ToLowerInvariant();
        Touch();
    }

    public void UpdateContact(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        Email = email.Trim();
        Touch();
    }

    public void UpdateBudget(decimal monthlyBudget)
    {
        if (monthlyBudget < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyBudget), monthlyBudget, "Monthly budget cannot be negative.");
        }

        MonthlyBudget = monthlyBudget;
        Touch();
    }

    public void UpdateCurrency(Currency preferredCurrency)
    {
        PreferredCurrency = preferredCurrency;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}