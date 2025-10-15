using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Dashboard.Models;

public sealed record DashboardSummary(
	decimal CurrentMonthExpenses,
	decimal CurrentMonthProfit,
	decimal RemainingBudget,
	int CategoriesUsed,
	int PendingAlerts,
	Currency Currency);