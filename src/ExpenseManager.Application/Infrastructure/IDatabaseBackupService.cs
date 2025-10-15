namespace ExpenseManager.Application.Infrastructure;

public interface IDatabaseBackupService
{
    Task CreateBackupAsync(string destinationPath, CancellationToken cancellationToken = default);
}
