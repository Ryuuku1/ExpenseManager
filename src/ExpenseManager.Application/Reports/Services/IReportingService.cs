using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Reports.Models;

namespace ExpenseManager.Application.Reports.Services;

public interface IReportingService
{
    Task<MonthlyReportSummary> GetMonthlySummaryAsync(Guid userId, DateOnly month, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SpendingByCategoryReportItem>> GetSpendingByCategoryAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MonthlySpendingTrendPoint>> GetMonthlySpendingTrendAsync(Guid userId, DateOnly startMonth, DateOnly endMonth, CancellationToken cancellationToken = default);
}
