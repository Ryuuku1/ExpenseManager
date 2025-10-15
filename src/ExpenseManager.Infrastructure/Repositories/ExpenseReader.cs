using ExpenseManager.Application.Expenses.Services;
using ExpenseManager.Application.ReceiptExpenseLinks.Models;
using ExpenseManager.Domain.Entities.Expenses;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Repositories;

internal sealed class ExpenseReader : IExpenseReader
{
    private readonly ExpenseManagerDbContext _dbContext;

    public ExpenseReader(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Expense?> GetAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses
            .Include(expense => expense.ReceiptLinks)
            .AsNoTracking()
            .FirstOrDefaultAsync(expense => expense.Id == expenseId && expense.UserId == userId, cancellationToken);
    }

    public async Task<ExpenseConnectionInfo?> GetConnectionInfoAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default)
    {
        var result = await (from expense in _dbContext.Expenses.AsNoTracking()
                            join category in _dbContext.Categories.AsNoTracking() on expense.CategoryId equals category.Id into categories
                            from category in categories.DefaultIfEmpty()
                            join link in _dbContext.ReceiptExpenseLinks.AsNoTracking() on expense.Id equals link.ExpenseId into linkGroup
                            where expense.UserId == userId && expense.Id == expenseId
                            select new
                            {
                                expense.Id,
                                expense.Title,
                                expense.CategoryId,
                                CategoryName = category != null ? category.Name : null,
                                expense.ExpenseDate,
                                LinkCount = linkGroup.Count()
                            }).FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new ExpenseConnectionInfo(
            result.Id,
            result.Title,
            result.CategoryId,
            result.CategoryName,
            result.ExpenseDate,
            result.LinkCount);
    }

    public async Task<IReadOnlyCollection<ExpenseConnectionInfo>> GetConnectionInfoAsync(
        Guid userId,
        IEnumerable<Guid> expenseIds,
        CancellationToken cancellationToken = default)
    {
        var ids = expenseIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<ExpenseConnectionInfo>();
        }

        var results = await (from expense in _dbContext.Expenses.AsNoTracking()
                             join category in _dbContext.Categories.AsNoTracking() on expense.CategoryId equals category.Id into categories
                             from category in categories.DefaultIfEmpty()
                             join link in _dbContext.ReceiptExpenseLinks.AsNoTracking() on expense.Id equals link.ExpenseId into linkGroup
                             where expense.UserId == userId && ids.Contains(expense.Id)
                             select new
                             {
                                 expense.Id,
                                 expense.Title,
                                 expense.CategoryId,
                                 CategoryName = category != null ? category.Name : null,
                                 expense.ExpenseDate,
                                 LinkCount = linkGroup.Count()
                             }).ToListAsync(cancellationToken);

        return results
            .Select(result => new ExpenseConnectionInfo(
                result.Id,
                result.Title,
                result.CategoryId,
                result.CategoryName,
                result.ExpenseDate,
                result.LinkCount))
            .ToList();
    }

    public async Task<IReadOnlyCollection<ExpenseConnectionInfo>> GetUnlinkedAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? categoryId,
        bool onlyOpenExpenses,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(expense => expense.ExpenseDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(expense => expense.ExpenseDate <= to.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(expense => expense.CategoryId == categoryId.Value);
        }

        if (onlyOpenExpenses)
        {
            query = query.Where(expense => expense.Status != ExpenseStatus.Paid);
        }

        var results = await (from expense in query
                             join category in _dbContext.Categories.AsNoTracking() on expense.CategoryId equals category.Id into categories
                             from category in categories.DefaultIfEmpty()
                             join link in _dbContext.ReceiptExpenseLinks.AsNoTracking() on expense.Id equals link.ExpenseId into linkGroup
                             where !linkGroup.Any()
                             orderby expense.ExpenseDate descending, expense.Id descending
                             select new
                             {
                                 expense.Id,
                                 expense.Title,
                                 expense.CategoryId,
                                 CategoryName = category != null ? category.Name : null,
                                 expense.ExpenseDate
                             }).ToListAsync(cancellationToken);

        return results
            .Select(result => new ExpenseConnectionInfo(
                result.Id,
                result.Title,
                result.CategoryId,
                result.CategoryName,
                result.ExpenseDate,
                0))
            .ToList();
    }

    public async Task<decimal> GetTotalAmountAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(expense => expense.ExpenseDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(expense => expense.ExpenseDate <= to.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(expense => expense.CategoryId == categoryId.Value);
        }

        return await query.SumAsync(expense => expense.Amount.Amount, cancellationToken);
    }

}
