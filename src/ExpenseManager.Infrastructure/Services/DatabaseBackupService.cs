using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Infrastructure;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class DatabaseBackupService : IDatabaseBackupService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseBackupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task CreateBackupAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Destination path is required.", nameof(destinationPath));
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExpenseManagerDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var connection = dbContext.Database.GetDbConnection();
        var sourcePath = connection.DataSource;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new InvalidOperationException("Não foi possível determinar a localização da base de dados.");
        }

        var fullSourcePath = Path.IsPathRooted(sourcePath) ? sourcePath : Path.GetFullPath(sourcePath);
        var targetDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        await using var sourceStream = new FileStream(fullSourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
    }
}
