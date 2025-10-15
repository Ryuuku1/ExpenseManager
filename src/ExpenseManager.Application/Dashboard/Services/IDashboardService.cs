using ExpenseManager.Application.Dashboard.Requests;
using ExpenseManager.Application.Dashboard.Responses;

namespace ExpenseManager.Application.Dashboard.Services;

public interface IDashboardService
{
    Task<GetDashboardOverviewResponse> GetOverviewAsync(GetDashboardOverviewRequest request, CancellationToken cancellationToken = default);
}