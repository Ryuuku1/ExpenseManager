using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Desktop.Services.Options;
using Microsoft.Extensions.Options;

namespace ExpenseManager.Desktop.Services;

internal sealed class FileAuthenticationStore : IAuthenticationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true
    };

    private readonly IOptionsMonitor<AuthenticationOptions> _optionsMonitor;
    private readonly IAppDataDirectoryProvider _appDataDirectoryProvider;
    private readonly string _filePath;

    public FileAuthenticationStore(IOptionsMonitor<AuthenticationOptions> optionsMonitor, IAppDataDirectoryProvider appDataDirectoryProvider)
    {
        _optionsMonitor = optionsMonitor;
        _appDataDirectoryProvider = appDataDirectoryProvider;

        var appData = _appDataDirectoryProvider.GetAppDataRoot();
        var directory = Path.Combine(appData, "ExpenseManager");
        _filePath = Path.Combine(directory, "authentication.json");
    }

    public async Task<IReadOnlyCollection<AuthenticationCredentials>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var model = await LoadAsync(cancellationToken);
        return model.Accounts;
    }

    public async Task<AuthenticationCredentials?> FindAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        var accounts = await GetAllAsync(cancellationToken);
        return accounts.FirstOrDefault(account => string.Equals(account.UserName, userName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task UpsertAsync(AuthenticationCredentials credentials, CancellationToken cancellationToken = default)
    {
        if (credentials is null)
        {
            throw new ArgumentNullException(nameof(credentials));
        }

        var model = await LoadAsync(cancellationToken);
        var updatedAccounts = model.Accounts
            .Where(account => !string.Equals(account.UserName, credentials.UserName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        updatedAccounts.Add(credentials);
        model.Accounts = updatedAccounts
            .OrderBy(account => account.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await SaveAsync(model, cancellationToken);
    }

    private async Task<AuthenticationStoreModel> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new AuthenticationStoreModel
            {
                Accounts = CreateDefaultAccounts()
            };
        }

        await using var stream = File.OpenRead(_filePath);
        var model = await JsonSerializer.DeserializeAsync<AuthenticationStoreModel>(stream, SerializerOptions, cancellationToken);

        if (model?.Accounts?.Count > 0)
        {
            return model;
        }

        stream.Position = 0;
        var legacyCredential = await JsonSerializer.DeserializeAsync<AuthenticationCredentials>(stream, SerializerOptions, cancellationToken);
        if (legacyCredential is null)
        {
            return new AuthenticationStoreModel
            {
                Accounts = CreateDefaultAccounts()
            };
        }

        return new AuthenticationStoreModel
        {
            Accounts = new List<AuthenticationCredentials> { legacyCredential }
        };
    }

    private List<AuthenticationCredentials> CreateDefaultAccounts()
    {
        var options = _optionsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
        {
            return new List<AuthenticationCredentials>();
        }

        var credential = AuthenticationCredentials.Create(options.Username, options.Password, options.DisplayName, options.UserId);
        return new List<AuthenticationCredentials> { credential };
    }

    private async Task SaveAsync(AuthenticationStoreModel model, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, model, SerializerOptions, cancellationToken);
    }

    private sealed class AuthenticationStoreModel
    {
        public List<AuthenticationCredentials> Accounts { get; set; } = new();
    }
}
