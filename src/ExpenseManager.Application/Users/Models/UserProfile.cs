using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Users.Models;

public sealed record UserProfile(
    Guid Id,
    string Name,
    string Email,
    decimal MonthlyBudget,
    string PreferredLanguage,
    Currency PreferredCurrency);
