using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Dashboard.Models;

public sealed record DashboardSummary(decimal CurrentMonthExpenses, decimal RemainingBudget, int CategoriesUsed, int PendingAlerts, Currency Currency);