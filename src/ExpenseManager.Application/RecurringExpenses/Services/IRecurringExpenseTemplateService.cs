using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.RecurringExpenses.Models;
using ExpenseManager.Application.RecurringExpenses.Requests;

namespace ExpenseManager.Application.RecurringExpenses.Services;

public interface IRecurringExpenseTemplateService
{
    Task<IReadOnlyCollection<RecurringExpenseTemplateItem>> GetTemplatesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid> CreateTemplateAsync(CreateRecurringExpenseTemplateRequest request, CancellationToken cancellationToken = default);
}
