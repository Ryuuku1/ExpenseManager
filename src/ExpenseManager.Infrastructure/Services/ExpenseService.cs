using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Expenses.Models;
using ExpenseManager.Application.Expenses.Requests;
using ExpenseManager.Application.Expenses.Services;
using ExpenseManager.Application.Expenses.Models.Shared;
using ExpenseManager.Domain.Entities.Expenses;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Domain.ValueObjects;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class ExpenseService : IExpenseService
{
    private readonly ExpenseManagerDbContext _dbContext;

    public ExpenseService(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ExpenseListItem>> GetExpensesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId)
            .Join(
                _dbContext.Categories.AsNoTracking(),
                expense => expense.CategoryId,
                category => category.Id,
                (expense, category) => new { expense, category })
            .OrderByDescending(result => result.expense.ExpenseDate)
            .ThenByDescending(result => result.expense.Id)
            .Select(result => new ExpenseListItem(
                result.expense.Id,
                result.expense.Title,
                result.category.Name,
                result.expense.Amount.Amount,
                result.expense.Amount.Currency,
                result.expense.ExpenseDate,
                result.expense.Status,
                result.expense.PaymentMethod,
                result.expense.DueDate,
                result.expense.Recurrence))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseDetails?> GetExpenseAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default)
    {
        var expense = await _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.Id == expenseId && expense.UserId == userId)
            .Join(
                _dbContext.Categories.AsNoTracking(),
                expense => expense.CategoryId,
                category => category.Id,
                (expense, category) => new { expense, category })
            .FirstOrDefaultAsync(cancellationToken);

        if (expense is null)
        {
            return null;
        }

        var attachments = await _dbContext.ExpenseReceipts
            .AsNoTracking()
            .Where(receipt => receipt.ExpenseId == expenseId)
            .OrderByDescending(receipt => receipt.UploadedAt)
            .Select(receipt => new ExpenseAttachmentItem(
                receipt.Id,
                receipt.ExpenseId,
                expense.expense.Title,
                receipt.FileName,
                receipt.FilePath,
                receipt.FileSizeInBytes,
                receipt.UploadedAt))
            .ToListAsync(cancellationToken);

        return new ExpenseDetails(
            expense.expense.Id,
            expense.expense.Title,
            expense.expense.Description,
            expense.expense.CategoryId,
            expense.category.Name,
            expense.expense.Amount.Amount,
            expense.expense.Amount.Currency,
            expense.expense.PaymentMethod,
            expense.expense.Status,
            expense.expense.ExpenseDate,
            expense.expense.DueDate,
            expense.expense.Recurrence,
            attachments);
    }

    public async Task<Guid> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var categoryExists = await _dbContext.Categories
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException("Category not found.");
        }

        var money = Money.Create(request.Amount, request.Currency);
        var expense = Expense.Create(
            request.UserId,
            request.CategoryId,
            request.Title,
            money,
            request.ExpenseDate,
            request.PaymentMethod,
            ExpenseStatus.Approved,
            request.Description,
            request.DueDate,
            request.Recurrence);

        foreach (var attachment in request.Attachments)
        {
            var attachmentEntity = ExpenseReceipt.Create(expense.Id, attachment.FileName, attachment.FilePath, attachment.FileSizeInBytes);
            expense.AddReceipt(attachmentEntity);
        }

        await _dbContext.Expenses.AddAsync(expense, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return expense.Id;
    }

    public async Task UpdateExpenseAsync(UpdateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = await _dbContext.Expenses
            .FirstOrDefaultAsync(expense => expense.Id == request.ExpenseId && expense.UserId == request.UserId, cancellationToken);

        if (expense is null)
        {
            throw new InvalidOperationException("Expense not found.");
        }

        var categoryExists = await _dbContext.Categories
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException("Category not found.");
        }

        var money = Money.Create(request.Amount, request.Currency);
        expense.UpdateDetails(request.Title, request.Description, request.CategoryId, money, request.PaymentMethod, request.ExpenseDate, request.DueDate, request.Recurrence);

        if (expense.Status != request.Status)
        {
            expense.ChangeStatus(request.Status);
        }

        foreach (var attachmentId in request.AttachmentIdsToRemove)
        {
            expense.RemoveReceipt(attachmentId);
        }

        foreach (var attachment in request.AttachmentsToAdd)
        {
            var attachmentEntity = ExpenseReceipt.Create(expense.Id, attachment.FileName, attachment.FilePath, attachment.FileSizeInBytes);
            expense.AddReceipt(attachmentEntity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteExpenseAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default)
    {
        var expense = await _dbContext.Expenses
            .Include(expense => expense.Receipts)
            .FirstOrDefaultAsync(expense => expense.Id == expenseId && expense.UserId == userId, cancellationToken);

        if (expense is null)
        {
            return;
        }

        _dbContext.Expenses.Remove(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

}
