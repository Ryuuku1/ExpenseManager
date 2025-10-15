using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Expenses.Models;
using ExpenseManager.Application.Expenses.Requests;

namespace ExpenseManager.Application.Expenses.Services;

public interface IExpenseService
{
    Task<IReadOnlyCollection<ExpenseListItem>> GetExpensesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ExpenseDetails?> GetExpenseAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default);

    Task<Guid> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);

    Task UpdateExpenseAsync(UpdateExpenseRequest request, CancellationToken cancellationToken = default);

    Task DeleteExpenseAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default);
}
