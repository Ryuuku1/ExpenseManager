using ExpenseManager.Application.Receipts.Models;
using ExpenseManager.Application.Receipts.Requests;

namespace ExpenseManager.Application.Receipts.Services;

public interface IReceiptService
{
    Task<IReadOnlyCollection<ReceiptListItem>> GetReceiptsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ReceiptDetails?> GetReceiptAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken = default);

    Task<Guid> CreateReceiptAsync(CreateReceiptRequest request, CancellationToken cancellationToken = default);

    Task UpdateReceiptAsync(UpdateReceiptRequest request, CancellationToken cancellationToken = default);

    Task DeleteReceiptAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken = default);
}
