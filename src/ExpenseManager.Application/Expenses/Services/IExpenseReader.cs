using ExpenseManager.Application.ReceiptExpenseLinks.Models;
using ExpenseManager.Domain.Entities.Expenses;

namespace ExpenseManager.Application.Expenses.Services;

public interface IExpenseReader
{
    Task<Expense?> GetAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default);

    Task<ExpenseConnectionInfo?> GetConnectionInfoAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExpenseConnectionInfo>> GetConnectionInfoAsync(
        Guid userId,
        IEnumerable<Guid> expenseIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExpenseConnectionInfo>> GetUnlinkedAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? categoryId,
        bool onlyOpenExpenses,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalAmountAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? categoryId,
        CancellationToken cancellationToken = default);
}
