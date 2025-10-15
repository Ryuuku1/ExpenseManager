using System;

namespace ExpenseManager.Application.Dashboard.Requests;

public sealed record GetDashboardOverviewRequest(Guid UserId, DateOnly Month);