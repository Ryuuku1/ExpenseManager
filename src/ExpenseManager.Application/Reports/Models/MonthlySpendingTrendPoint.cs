using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Reports.Models;

public sealed record MonthlySpendingTrendPoint(DateOnly Month, decimal TotalAmount, Currency Currency);
