using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Users.Services;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class UserReadService : IUserReadService
{
    private readonly ExpenseManagerDbContext _dbContext;

    public UserReadService(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetDefaultUserIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return user?.Id;
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);
    }
}