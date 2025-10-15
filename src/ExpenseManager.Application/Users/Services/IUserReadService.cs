using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExpenseManager.Application.Users.Services;

public interface IUserReadService
{
    Task<Guid?> GetDefaultUserIdAsync(CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}