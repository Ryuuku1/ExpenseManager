using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Dashboard.Models;

public sealed record DashboardExpenseItem(Guid ExpenseId, string Title, string CategoryName, decimal Amount, Currency Currency, DateOnly ExpenseDate, ExpenseStatus Status);