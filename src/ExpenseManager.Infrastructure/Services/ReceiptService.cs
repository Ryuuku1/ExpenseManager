using ExpenseManager.Application.Receipts.Models;
using ExpenseManager.Application.Receipts.Requests;
using ExpenseManager.Application.Receipts.Services;
using ExpenseManager.Domain.Entities.Receipts;
using ExpenseManager.Domain.ValueObjects;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class ReceiptService : IReceiptService
{
    private readonly ExpenseManagerDbContext _dbContext;

    public ReceiptService(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ReceiptListItem>> GetReceiptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var query = from receipt in _dbContext.Receipts.AsNoTracking()
                    where receipt.UserId == userId
                    join category in _dbContext.Categories.AsNoTracking() on receipt.CategoryId equals category.Id into categories
                    from category in categories.DefaultIfEmpty()
                    orderby receipt.ReceiptDate descending, receipt.Id descending
                    select new ReceiptListItem(
                        receipt.Id,
                        receipt.Title,
                        receipt.CategoryId,
                        category != null ? category.Name : null,
                        receipt.ReceiptDate,
                        receipt.NetAmount.Amount,
                        receipt.NetAmount.Currency,
                        receipt.Vendor,
                        receipt.ReferenceNumber,
                        receipt.CommissionPercent);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ReceiptDetails?> GetReceiptAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken = default)
    {
        var query = from receipt in _dbContext.Receipts.AsNoTracking()
                    where receipt.UserId == userId && receipt.Id == receiptId
                    join category in _dbContext.Categories.AsNoTracking() on receipt.CategoryId equals category.Id into categories
                    from category in categories.DefaultIfEmpty()
                    select new ReceiptDetails(
                        receipt.Id,
                        receipt.UserId,
                        receipt.CategoryId,
                        category != null ? category.Name : null,
                        receipt.Title,
                        receipt.Description,
                        receipt.ReferenceNumber,
                        receipt.Vendor,
                        receipt.ReceiptDate,
                        receipt.NetAmount.Amount,
                        receipt.NetAmount.Currency,
                        receipt.CommissionPercent,
                        receipt.FileName,
                        receipt.FilePath,
                        receipt.FileSizeInBytes,
                        receipt.CapturedAt,
                        receipt.UpdatedAt);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateReceiptAsync(CreateReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(category => category.Id == request.CategoryId.Value, cancellationToken);

            if (!categoryExists)
            {
                throw new InvalidOperationException("Category not found.");
            }
        }

        var netAmount = Money.Create(request.NetAmount, request.Currency);
        var receipt = Receipt.Create(
            request.UserId,
            request.Title,
            netAmount,
            request.ReceiptDate,
            request.CategoryId,
            request.Description,
            request.ReferenceNumber,
            request.Vendor,
            request.CommissionPercent,
            request.FileName,
            request.FilePath,
            request.FileSizeInBytes);

        await _dbContext.Receipts.AddAsync(receipt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return receipt.Id;
    }

    public async Task UpdateReceiptAsync(UpdateReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.Receipts
            .FirstOrDefaultAsync(r => r.Id == request.ReceiptId && r.UserId == request.UserId, cancellationToken);

        if (receipt is null)
        {
            throw new InvalidOperationException("Receipt not found.");
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(category => category.Id == request.CategoryId.Value, cancellationToken);

            if (!categoryExists)
            {
                throw new InvalidOperationException("Category not found.");
            }
        }

        var netAmount = Money.Create(request.NetAmount, request.Currency);
        receipt.UpdateDetails(
            request.Title,
            netAmount,
            request.ReceiptDate,
            request.CategoryId,
            request.Description,
            request.ReferenceNumber,
            request.Vendor,
            request.CommissionPercent);

        receipt.UpdateFileMetadata(request.FileName, request.FilePath, request.FileSizeInBytes);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteReceiptAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.Receipts
            .Include(r => r.ExpenseLinks)
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.UserId == userId, cancellationToken);

        if (receipt is null)
        {
            return;
        }

        _dbContext.Receipts.Remove(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
