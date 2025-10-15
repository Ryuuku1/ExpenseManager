using System.Threading;
using System.Threading.Tasks;

namespace ExpenseManager.Application.Infrastructure;

public interface IDatabaseBackupService
{
    Task CreateBackupAsync(string destinationPath, CancellationToken cancellationToken = default);
}
