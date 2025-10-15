using ExpenseManager.Application.ReceiptExpenseLinks.Repositories;
using ExpenseManager.Domain.Entities.Links;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Repositories;

internal sealed class ReceiptExpenseLinkWriter : IReceiptExpenseLinkWriter
{
    private readonly ExpenseManagerDbContext _dbContext;

    public ReceiptExpenseLinkWriter(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReceiptExpenseLink> CreateAsync(ReceiptExpenseLink link, CancellationToken cancellationToken = default)
    {
        await _dbContext.ReceiptExpenseLinks.AddAsync(link, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return link;
    }

    public async Task UpdateAsync(ReceiptExpenseLink link, CancellationToken cancellationToken = default)
    {
        // EF Core can throw if another instance with the same key is already tracked.
        // Detach any existing local instance before attaching the provided detached instance.
        var local = _dbContext.ReceiptExpenseLinks.Local.FirstOrDefault(e => e.Id == link.Id);
        if (local is not null)
        {
            _dbContext.Entry(local).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }

        _dbContext.Attach(link);
        _dbContext.Entry(link).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var link = await _dbContext.ReceiptExpenseLinks
            .FirstOrDefaultAsync(entity => entity.Id == linkId, cancellationToken);

        if (link is null)
        {
            return;
        }

        _dbContext.ReceiptExpenseLinks.Remove(link);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
