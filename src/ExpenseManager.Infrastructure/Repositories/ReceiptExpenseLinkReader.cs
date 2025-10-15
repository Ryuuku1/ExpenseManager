using ExpenseManager.Application.ReceiptExpenseLinks.Repositories;
using ExpenseManager.Domain.Entities.Links;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Repositories;

internal sealed class ReceiptExpenseLinkReader : IReceiptExpenseLinkReader
{
    private readonly ExpenseManagerDbContext _dbContext;

    public ReceiptExpenseLinkReader(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReceiptExpenseLink?> GetAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ReceiptExpenseLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.Id == linkId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReceiptExpenseLink>> GetByExpenseAsync(
        Guid userId,
        Guid expenseId,
        DateOnly? from,
        DateOnly? to,
        Guid? receiptCategoryId,
        CancellationToken cancellationToken = default)
    {
        var query = from link in _dbContext.ReceiptExpenseLinks.AsNoTracking()
                    join expense in _dbContext.Expenses.AsNoTracking() on link.ExpenseId equals expense.Id
                    join receipt in _dbContext.Receipts.AsNoTracking() on link.ReceiptId equals receipt.Id
                    where expense.UserId == userId
                        && receipt.UserId == userId
                        && link.ExpenseId == expenseId
                    select new { link, receipt };

        if (from.HasValue)
        {
            query = query.Where(item => item.receipt.ReceiptDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(item => item.receipt.ReceiptDate <= to.Value);
        }

        if (receiptCategoryId.HasValue)
        {
            query = query.Where(item => item.receipt.CategoryId == receiptCategoryId.Value);
        }

        return await query
            .OrderByDescending(item => item.receipt.ReceiptDate)
            .ThenByDescending(item => item.link.CreatedAt())
            .Select(item => item.link)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReceiptExpenseLink>> GetByReceiptAsync(
        Guid userId,
        Guid receiptId,
        DateOnly? from,
        DateOnly? to,
        Guid? expenseCategoryId,
        CancellationToken cancellationToken = default)
    {
        var query = from link in _dbContext.ReceiptExpenseLinks.AsNoTracking()
                    join receipt in _dbContext.Receipts.AsNoTracking() on link.ReceiptId equals receipt.Id
                    join expense in _dbContext.Expenses.AsNoTracking() on link.ExpenseId equals expense.Id
                    where expense.UserId == userId
                        && receipt.UserId == userId
                        && link.ReceiptId == receiptId
                    select new { link, expense };

        if (from.HasValue)
        {
            query = query.Where(item => item.expense.ExpenseDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(item => item.expense.ExpenseDate <= to.Value);
        }

        if (expenseCategoryId.HasValue)
        {
            query = query.Where(item => item.expense.CategoryId == expenseCategoryId.Value);
        }

        return await query
            .OrderByDescending(item => item.expense.ExpenseDate)
            .ThenByDescending(item => item.link.CreatedAt())
            .Select(item => item.link)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid expenseId, Guid receiptId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ReceiptExpenseLinks
            .AsNoTracking()
            .AnyAsync(link => link.ExpenseId == expenseId && link.ReceiptId == receiptId, cancellationToken);
    }
}

internal static class ReceiptExpenseLinkExtensions
{
    public static DateTime CreatedAt(this ReceiptExpenseLink link)
    {
        return link.AuditTrail.CreatedAt;
    }
}
