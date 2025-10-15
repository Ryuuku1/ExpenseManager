using System.Collections.Generic;
using ExpenseManager.Application.Dashboard.Models;

namespace ExpenseManager.Application.Dashboard.Responses;

public sealed record GetDashboardOverviewResponse(
    DashboardSummary Summary,
    IReadOnlyCollection<DashboardExpenseItem> RecentExpenses,
    IReadOnlyCollection<DashboardTrendPoint> SpendingTrend,
    IReadOnlyCollection<DashboardQuickAction> QuickActions,
    IReadOnlyCollection<DashboardAlertItem> Alerts);