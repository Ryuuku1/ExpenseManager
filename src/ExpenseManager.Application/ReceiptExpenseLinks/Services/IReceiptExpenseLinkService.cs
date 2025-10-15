using ExpenseManager.Application.ReceiptExpenseLinks.Requests;
using ExpenseManager.Application.ReceiptExpenseLinks.Responses;

namespace ExpenseManager.Application.ReceiptExpenseLinks.Services;

public interface IReceiptExpenseLinkService
{
    Task<ReceiptExpenseLinkDetailResponse> CreateAsync(CreateReceiptExpenseLinkRequest request, CancellationToken cancellationToken = default);

    Task<ReceiptExpenseLinkDetailResponse> UpdateAsync(UpdateReceiptExpenseLinkRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteReceiptExpenseLinkRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReceiptExpenseLinkDetailResponse>> GetByExpenseAsync(GetLinksByExpenseIdRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReceiptExpenseLinkDetailResponse>> GetByReceiptAsync(GetLinksByReceiptIdRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LinkSummaryResponse>> GetUnlinkedExpensesAsync(GetUnlinkedExpensesRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LinkSummaryResponse>> GetUnlinkedReceiptsAsync(GetUnlinkedReceiptsRequest request, CancellationToken cancellationToken = default);

    Task<ConnectionsSummaryResponse> GetSummaryAsync(GetConnectionsSummaryRequest request, CancellationToken cancellationToken = default);

    // Returns all links for the specified user, optionally filtered by date range and categories.
    Task<IReadOnlyCollection<ReceiptExpenseLinkDetailResponse>> GetAllAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? expenseCategoryId,
        Guid? receiptCategoryId,
        CancellationToken cancellationToken = default);
}
