using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Reports.Models;

public sealed record MonthlyReportSummary(
    DateOnly Month,
    decimal TotalExpenses,
    decimal AverageExpense,
    decimal LargestExpense,
    int ExpenseCount,
    decimal MonthlyBudget,
    decimal BudgetRemaining,
    Currency Currency);
