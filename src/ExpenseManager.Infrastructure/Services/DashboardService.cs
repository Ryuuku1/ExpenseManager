using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Calendar.Services;
using ExpenseManager.Application.Dashboard.Models;
using ExpenseManager.Application.Dashboard.Requests;
using ExpenseManager.Application.Dashboard.Responses;
using ExpenseManager.Application.Dashboard.Services;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class DashboardService : IDashboardService
{
    private readonly ExpenseManagerDbContext _dbContext;
    private readonly ICalendarService _calendarService;

    public DashboardService(ExpenseManagerDbContext dbContext, ICalendarService calendarService)
    {
        _dbContext = dbContext;
        _calendarService = calendarService;
    }

    public async Task<GetDashboardOverviewResponse> GetOverviewAsync(GetDashboardOverviewRequest request, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(request.Month.Year, request.Month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return CreateEmptyResponse(Currency.Eur);
        }

        var monthExpensesQuery = _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == request.UserId && expense.ExpenseDate >= monthStart && expense.ExpenseDate <= monthEnd);

        var currentMonthExpenses = await monthExpensesQuery
            .SumAsync(expense => expense.Amount.Amount, cancellationToken);

        var categoriesUsed = await monthExpensesQuery
            .Select(expense => expense.CategoryId)
            .Distinct()
            .CountAsync(cancellationToken);

        var reminders = await _calendarService.GetDashboardRemindersAsync(request.UserId, cancellationToken);
        var pendingAlerts = reminders.Count;

        var recentExpenses = await _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == request.UserId)
            .OrderByDescending(expense => expense.ExpenseDate)
            .ThenByDescending(expense => expense.CreatedAt)
            .Take(5)
            .Join(
                _dbContext.Categories.AsNoTracking(),
                expense => expense.CategoryId,
                category => category.Id,
                (expense, category) => new DashboardExpenseItem(
                    expense.Id,
                    expense.Title,
                    category.Name,
                    expense.Amount.Amount,
                    expense.Amount.Currency,
                    expense.ExpenseDate,
                    expense.Status))
            .ToListAsync(cancellationToken);

        var spendingTrend = await monthExpensesQuery
            .GroupBy(expense => expense.ExpenseDate)
            .OrderBy(group => group.Key)
            .Select(group => new DashboardTrendPoint(
                group.Key,
                group.Sum(expense => expense.Amount.Amount)))
            .ToListAsync(cancellationToken);

        var alerts = reminders
            .OrderBy(item => item.OccursAt)
            .Take(5)
            .ToList();

        var remainingBudget = Math.Max(0, user.MonthlyBudget - currentMonthExpenses);

        var summary = new DashboardSummary(
            currentMonthExpenses,
            remainingBudget,
            categoriesUsed,
            pendingAlerts,
            user.PreferredCurrency);

        return new GetDashboardOverviewResponse(
            summary,
            recentExpenses,
            spendingTrend,
            GetQuickActions(),
            alerts);
    }

    private static IReadOnlyCollection<DashboardQuickAction> GetQuickActions()
    {
        return new List<DashboardQuickAction>
        {
            new("DASHBOARD_QUICK_ACTION_ADD_EXPENSE", "\uE710", "AddExpense"),
            new("DASHBOARD_QUICK_ACTION_VIEW_EXPENSES", "\uE8A7", "ViewExpenses"),
            new("DASHBOARD_QUICK_ACTION_MANAGE_CATEGORIES", "\uE8FD", "ManageCategories"),
            new("DASHBOARD_QUICK_ACTION_SCHEDULE_EVENT", "\uE163", "AddCalendarEvent")
        };
    }

    private static GetDashboardOverviewResponse CreateEmptyResponse(Currency currency)
    {
        var summary = new DashboardSummary(0, 0, 0, 0, currency);

        return new GetDashboardOverviewResponse(
            summary,
            Array.Empty<DashboardExpenseItem>(),
            Array.Empty<DashboardTrendPoint>(),
            GetQuickActions(),
            Array.Empty<DashboardAlertItem>());
    }
}