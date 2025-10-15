using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Users.Requests;

public sealed record UpdateUserProfileRequest(
    Guid UserId,
    string Name,
    string Email,
    decimal MonthlyBudget,
    string PreferredLanguage,
    Currency PreferredCurrency);
