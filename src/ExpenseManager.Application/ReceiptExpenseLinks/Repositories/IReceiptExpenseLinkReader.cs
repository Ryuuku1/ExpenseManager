using ExpenseManager.Domain.Entities.Links;

namespace ExpenseManager.Application.ReceiptExpenseLinks.Repositories;

public interface IReceiptExpenseLinkReader
{
    Task<ReceiptExpenseLink?> GetAsync(Guid linkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReceiptExpenseLink>> GetByExpenseAsync(
        Guid userId,
        Guid expenseId,
        DateOnly? from,
        DateOnly? to,
        Guid? receiptCategoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReceiptExpenseLink>> GetByReceiptAsync(
        Guid userId,
        Guid receiptId,
        DateOnly? from,
        DateOnly? to,
        Guid? expenseCategoryId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid expenseId, Guid receiptId, CancellationToken cancellationToken = default);
}
