using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.RecurringExpenses.Models;
using ExpenseManager.Application.RecurringExpenses.Requests;
using ExpenseManager.Application.RecurringExpenses.Services;
using ExpenseManager.Domain.Entities.Expenses;
using ExpenseManager.Domain.ValueObjects;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class RecurringExpenseTemplateService : IRecurringExpenseTemplateService
{
    private readonly ExpenseManagerDbContext _dbContext;

    public RecurringExpenseTemplateService(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<RecurringExpenseTemplateItem>> GetTemplatesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RecurringExpenseTemplates
            .AsNoTracking()
            .Where(template => template.UserId == userId)
            .Join(
                _dbContext.Categories.AsNoTracking(),
                template => template.CategoryId,
                category => category.Id,
                (template, category) => new RecurringExpenseTemplateItem(
                    template.Id,
                    template.Name,
                    category.Name,
                    template.Amount.Amount,
                    template.Amount.Currency,
                    template.Recurrence,
                    template.PaymentMethod,
                    template.StartDate,
                    template.EndDate))
            .OrderBy(template => template.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateTemplateAsync(CreateRecurringExpenseTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var userExists = await _dbContext.Users.AnyAsync(user => user.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException("User not found.");
        }

        var categoryExists = await _dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new InvalidOperationException("Category not found.");
        }

        var amount = Money.Create(request.Amount, request.Currency);
        var template = RecurringExpenseTemplate.Create(
            request.UserId,
            request.CategoryId,
            request.Name,
            amount,
            request.Recurrence,
            request.PaymentMethod,
            request.StartDate,
            request.EndDate,
            request.Notes);

        await _dbContext.RecurringExpenseTemplates.AddAsync(template, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}
