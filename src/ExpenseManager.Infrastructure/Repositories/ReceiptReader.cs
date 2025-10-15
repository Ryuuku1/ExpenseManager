using ExpenseManager.Application.ReceiptExpenseLinks.Models;
using ExpenseManager.Application.Receipts.Services;
using ExpenseManager.Domain.Entities.Receipts;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Repositories;

internal sealed class ReceiptReader : IReceiptReader
{
    private readonly ExpenseManagerDbContext _dbContext;

    public ReceiptReader(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Receipt?> GetAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Receipts
            .Include(receipt => receipt.ExpenseLinks)
            .AsNoTracking()
            .FirstOrDefaultAsync(receipt => receipt.Id == receiptId && receipt.UserId == userId, cancellationToken);
    }

    public async Task<ReceiptConnectionInfo?> GetConnectionInfoAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken = default)
    {
        var result = await (from receipt in _dbContext.Receipts.AsNoTracking()
                            join category in _dbContext.Categories.AsNoTracking() on receipt.CategoryId equals category.Id into categories
                            from category in categories.DefaultIfEmpty()
                            join link in _dbContext.ReceiptExpenseLinks.AsNoTracking() on receipt.Id equals link.ReceiptId into linkGroup
                            where receipt.UserId == userId && receipt.Id == receiptId
                            select new
                            {
                                receipt.Id,
                                receipt.Title,
                                receipt.CategoryId,
                                CategoryName = category != null ? category.Name : null,
                                receipt.ReceiptDate,
                                LinkCount = linkGroup.Count(),
                                receipt.CommissionPercent
                            }).FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new ReceiptConnectionInfo(
            result.Id,
            result.Title,
            result.CategoryId,
            result.CategoryName,
            result.ReceiptDate,
            result.LinkCount,
            result.CommissionPercent);
    }

    public async Task<IReadOnlyCollection<ReceiptConnectionInfo>> GetConnectionInfoAsync(
        Guid userId,
        IEnumerable<Guid> receiptIds,
        CancellationToken cancellationToken = default)
    {
        var ids = receiptIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<ReceiptConnectionInfo>();
        }

        var results = await (from receipt in _dbContext.Receipts.AsNoTracking()
                             join category in _dbContext.Categories.AsNoTracking() on receipt.CategoryId equals category.Id into categories
                             from category in categories.DefaultIfEmpty()
                             join link in _dbContext.ReceiptExpenseLinks.AsNoTracking() on receipt.Id equals link.ReceiptId into linkGroup
                             where receipt.UserId == userId && ids.Contains(receipt.Id)
                             select new
                             {
                                 receipt.Id,
                                 receipt.Title,
                                 receipt.CategoryId,
                                 CategoryName = category != null ? category.Name : null,
                                 receipt.ReceiptDate,
                                 LinkCount = linkGroup.Count(),
                                 receipt.CommissionPercent
                             }).ToListAsync(cancellationToken);

        return results
            .Select(result => new ReceiptConnectionInfo(
                result.Id,
                result.Title,
                result.CategoryId,
                result.CategoryName,
                result.ReceiptDate,
                result.LinkCount,
                result.CommissionPercent))
            .ToList();
    }

    public async Task<IReadOnlyCollection<ReceiptConnectionInfo>> GetUnlinkedAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Receipts
            .AsNoTracking()
            .Where(receipt => receipt.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(receipt => receipt.ReceiptDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(receipt => receipt.ReceiptDate <= to.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(receipt => receipt.CategoryId == categoryId.Value);
        }

        var results = await (from receipt in query
                             join category in _dbContext.Categories.AsNoTracking() on receipt.CategoryId equals category.Id into categories
                             from category in categories.DefaultIfEmpty()
                             join link in _dbContext.ReceiptExpenseLinks.AsNoTracking() on receipt.Id equals link.ReceiptId into linkGroup
                             where !linkGroup.Any()
                             orderby receipt.ReceiptDate descending, receipt.Id descending
                             select new
                             {
                                 receipt.Id,
                                 receipt.Title,
                                 receipt.CategoryId,
                                 CategoryName = category != null ? category.Name : null,
                                 receipt.ReceiptDate,
                                 receipt.CommissionPercent
                             }).ToListAsync(cancellationToken);

        return results
            .Select(result => new ReceiptConnectionInfo(
                result.Id,
                result.Title,
                result.CategoryId,
                result.CategoryName,
                result.ReceiptDate,
                0,
                result.CommissionPercent))
            .ToList();
    }

    public async Task<decimal> GetTotalNetAmountAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Receipts
            .AsNoTracking()
            .Where(receipt => receipt.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(receipt => receipt.ReceiptDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(receipt => receipt.ReceiptDate <= to.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(receipt => receipt.CategoryId == categoryId.Value);
        }

        return await query.SumAsync(receipt => receipt.NetAmount.Amount, cancellationToken);
    }
}
