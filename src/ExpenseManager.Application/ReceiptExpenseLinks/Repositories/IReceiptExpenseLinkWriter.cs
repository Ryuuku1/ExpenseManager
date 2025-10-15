using ExpenseManager.Domain.Entities.Links;

namespace ExpenseManager.Application.ReceiptExpenseLinks.Repositories;

public interface IReceiptExpenseLinkWriter
{
    Task<ReceiptExpenseLink> CreateAsync(ReceiptExpenseLink link, CancellationToken cancellationToken = default);

    Task UpdateAsync(ReceiptExpenseLink link, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid linkId, CancellationToken cancellationToken = default);
}
