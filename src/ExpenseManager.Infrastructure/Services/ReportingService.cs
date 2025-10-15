using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Reports.Models;
using ExpenseManager.Application.Reports.Services;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class ReportingService : IReportingService
{
    private readonly ExpenseManagerDbContext _dbContext;

    public ReportingService(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MonthlyReportSummary> GetMonthlySummaryAsync(Guid userId, DateOnly month, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var expensesQuery = _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId && expense.ExpenseDate >= monthStart && expense.ExpenseDate <= monthEnd);

        var total = await expensesQuery.SumAsync(expense => expense.Amount.Amount, cancellationToken);
        var count = await expensesQuery.CountAsync(cancellationToken);
        var largest = 0m;
        if (count > 0)
        {
            var largestAsDouble = await expensesQuery
                .Select(expense => (double)expense.Amount.Amount)
                .OrderByDescending(amount => amount)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            largest = (decimal)largestAsDouble;
        }
        var average = count > 0 ? total / count : 0m;

        var user = await _dbContext.Users.AsNoTracking().FirstAsync(user => user.Id == userId, cancellationToken);
        var budgetRemaining = Math.Max(0, user.MonthlyBudget - total);

        return new MonthlyReportSummary(
            monthStart,
            decimal.Round(total, 2),
            decimal.Round(average, 2),
            decimal.Round(largest, 2),
            count,
            user.MonthlyBudget,
            decimal.Round(budgetRemaining, 2),
            user.PreferredCurrency);
    }

    public async Task<IReadOnlyCollection<SpendingByCategoryReportItem>> GetSpendingByCategoryAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var grouped = await _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId && expense.ExpenseDate >= startDate && expense.ExpenseDate <= endDate)
            .Join(
                _dbContext.Categories.AsNoTracking(),
                expense => expense.CategoryId,
                category => category.Id,
                (expense, category) => new
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    AmountValue = expense.Amount.Amount,
                    CurrencyCode = expense.Amount.Currency
                })
            .GroupBy(entry => new { entry.CategoryId, entry.CategoryName, entry.CurrencyCode })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Key.CurrencyCode,
                TotalAmount = group.Sum(item => item.AmountValue),
                ExpenseCount = group.Count()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return grouped
            .OrderByDescending(item => item.TotalAmount)
            .Select(item => new SpendingByCategoryReportItem(item.CategoryId, item.CategoryName, item.TotalAmount, item.CurrencyCode, item.ExpenseCount))
            .ToList();
    }

    public async Task<IReadOnlyCollection<MonthlySpendingTrendPoint>> GetMonthlySpendingTrendAsync(Guid userId, DateOnly startMonth, DateOnly endMonth, CancellationToken cancellationToken = default)
    {
        if (endMonth < startMonth)
        {
            (startMonth, endMonth) = (endMonth, startMonth);
        }

        var periodStart = new DateOnly(startMonth.Year, startMonth.Month, 1);
        var periodEnd = new DateOnly(endMonth.Year, endMonth.Month, 1).AddMonths(1).AddDays(-1);

        var user = await _dbContext.Users.AsNoTracking().FirstAsync(user => user.Id == userId, cancellationToken);

        var grouped = await _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId && expense.ExpenseDate >= periodStart && expense.ExpenseDate <= periodEnd)
            .GroupBy(expense => new { expense.ExpenseDate.Year, expense.ExpenseDate.Month, expense.Amount.Currency })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Currency,
                Total = group.Sum(expense => expense.Amount.Amount)
            })
            .ToListAsync(cancellationToken);

        var results = new List<MonthlySpendingTrendPoint>();

        var cursor = new DateOnly(startMonth.Year, startMonth.Month, 1);
        var finalMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);

        while (cursor <= finalMonth)
        {
            var monthData = grouped.FirstOrDefault(item => item.Year == cursor.Year && item.Month == cursor.Month);
            var total = monthData?.Total ?? 0m;
            var currency = monthData?.Currency ?? user.PreferredCurrency;
            results.Add(new MonthlySpendingTrendPoint(cursor, decimal.Round(total, 2), currency));
            cursor = cursor.AddMonths(1);
        }

        return results;
    }
}
