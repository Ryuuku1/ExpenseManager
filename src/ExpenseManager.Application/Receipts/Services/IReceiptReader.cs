using ExpenseManager.Application.ReceiptExpenseLinks.Models;
using ExpenseManager.Domain.Entities.Receipts;

namespace ExpenseManager.Application.Receipts.Services;

public interface IReceiptReader
{
    Task<Receipt?> GetAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken = default);

    Task<ReceiptConnectionInfo?> GetConnectionInfoAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReceiptConnectionInfo>> GetConnectionInfoAsync(
        Guid userId,
        IEnumerable<Guid> receiptIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReceiptConnectionInfo>> GetUnlinkedAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? categoryId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalNetAmountAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? categoryId,
        CancellationToken cancellationToken = default);
}
