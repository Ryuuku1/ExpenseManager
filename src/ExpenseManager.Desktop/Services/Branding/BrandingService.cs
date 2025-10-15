using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExpenseManager.Desktop.Services.Options;
using WpfApplication = System.Windows.Application;

namespace ExpenseManager.Desktop.Services.Branding;

public enum BrandingColorScheme
{
    Midnight,
    Emerald,
    Sunset,
    Aurora,
    Ocean,
    Pinky,
    Salmon,
    Blush,
    BlushLight
}

public sealed record BrandingSettings(string? LogoPath, string? IconPath, BrandingColorScheme ColorScheme);

public sealed record BrandingUpdate(string? LogoPath, bool ReplaceLogo, string? IconPath, bool ReplaceIcon, BrandingColorScheme ColorScheme);

public interface IBrandingService
{
    BrandingSettings Current { get; }

    event EventHandler? BrandingChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(BrandingUpdate update, CancellationToken cancellationToken = default);
}

public sealed class BrandingService : IBrandingService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly IReadOnlyDictionary<BrandingColorScheme, Uri> PaletteUris = new Dictionary<BrandingColorScheme, Uri>
    {
        [BrandingColorScheme.Midnight] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.Midnight.xaml", UriKind.Relative),
        [BrandingColorScheme.Emerald] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.Emerald.xaml", UriKind.Relative),
        [BrandingColorScheme.Sunset] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.Sunset.xaml", UriKind.Relative),
        [BrandingColorScheme.Aurora] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.Aurora.xaml", UriKind.Relative),
        [BrandingColorScheme.Ocean] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.Ocean.xaml", UriKind.Relative),
        [BrandingColorScheme.Pinky] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.Pinky.xaml", UriKind.Relative),
        [BrandingColorScheme.Salmon] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.Salmon.xaml", UriKind.Relative),
        [BrandingColorScheme.Blush] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.Blush.xaml", UriKind.Relative),
        [BrandingColorScheme.BlushLight] = new("/ExpenseManager.Desktop;component/Resources/Palettes/Palette.BlushLight.xaml", UriKind.Relative)
    };

    private readonly ILogger<BrandingService> _logger;
    private readonly string _brandingDirectory;
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ResourceDictionary? _activePaletteDictionary;

    private BrandingSettings _current;
    private bool _initialized;

    public BrandingService(IOptions<BrandingOptions> options, ILogger<BrandingService> logger)
    {
        _logger = logger;
        var defaults = options.Value;
        _current = new BrandingSettings(defaults.LogoPath, defaults.IconPath, defaults.ColorScheme);

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _brandingDirectory = Path.Combine(appData, "ExpenseManager", "branding");
        _settingsPath = Path.Combine(_brandingDirectory, "branding.json");
    }

    public BrandingSettings Current => _current;

    public event EventHandler? BrandingChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            Directory.CreateDirectory(_brandingDirectory);

            var settings = await TryLoadSettingsAsync(cancellationToken).ConfigureAwait(false) ?? _current;

            if (!string.IsNullOrWhiteSpace(settings.LogoPath) && !File.Exists(settings.LogoPath))
            {
                _logger.LogWarning("Stored logo path '{LogoPath}' not found. Resetting to default.", settings.LogoPath);
                settings = settings with { LogoPath = null };
            }

            if (!string.IsNullOrWhiteSpace(settings.IconPath) && !File.Exists(settings.IconPath))
            {
                _logger.LogWarning("Stored icon path '{IconPath}' not found. Resetting to default.", settings.IconPath);
                settings = settings with { IconPath = null };
            }

            _current = settings;
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }

        await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            ApplyPalette(_current.ColorScheme);
            BrandingChanged?.Invoke(this, EventArgs.Empty);
        }).Task.ConfigureAwait(false);
    }

    public async Task UpdateAsync(BrandingUpdate update, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string? logoPath = _current.LogoPath;
            string? iconPath = _current.IconPath;

            if (update.ReplaceLogo)
            {
                if (!string.IsNullOrWhiteSpace(update.LogoPath))
                {
                    logoPath = await PersistLogoAsync(update.LogoPath!, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    RemoveExistingLogo();
                    logoPath = null;
                }
            }

            if (update.ReplaceIcon)
            {
                if (!string.IsNullOrWhiteSpace(update.IconPath))
                {
                    iconPath = await PersistIconAsync(update.IconPath!, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    RemoveExistingIcon();
                    iconPath = null;
                }
            }

            var newSettings = new BrandingSettings(logoPath, iconPath, update.ColorScheme);
            await PersistSettingsAsync(newSettings, cancellationToken).ConfigureAwait(false);
            _current = newSettings;
        }
        finally
        {
            _gate.Release();
        }

        await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            ApplyPalette(_current.ColorScheme);
            BrandingChanged?.Invoke(this, EventArgs.Empty);
        }).Task.ConfigureAwait(false);
    }

    private async Task<BrandingSettings?> TryLoadSettingsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsPath);
            var dto = await JsonSerializer.DeserializeAsync<BrandingSettingsDto>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            if (dto is null)
            {
                return null;
            }

            if (!Enum.TryParse<BrandingColorScheme>(dto.ColorScheme, ignoreCase: true, out var scheme))
            {
                scheme = BrandingColorScheme.Midnight;
            }

            return new BrandingSettings(dto.LogoPath, dto.IconPath, scheme);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load branding settings.");
            return null;
        }
    }

    private async Task PersistSettingsAsync(BrandingSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_brandingDirectory);
            await using var stream = File.Create(_settingsPath);
            var dto = new BrandingSettingsDto(settings.LogoPath, settings.IconPath, settings.ColorScheme.ToString());
            await JsonSerializer.SerializeAsync(stream, dto, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to persist branding settings.");
            throw;
        }
    }

    private async Task<string> PersistLogoAsync(string sourcePath, CancellationToken cancellationToken)
    {
        return await PersistAssetAsync(sourcePath, "logo", ".png", cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> PersistIconAsync(string sourcePath, CancellationToken cancellationToken)
    {
        return await PersistAssetAsync(sourcePath, "icon", ".ico", cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> PersistAssetAsync(string sourcePath, string prefix, string defaultExtension, CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Selected {prefix} file was not found.", sourcePath);
        }

        Directory.CreateDirectory(_brandingDirectory);

        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = defaultExtension;
        }

        var destinationFileName = $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension.ToLowerInvariant()}";
        var destinationPath = Path.Combine(_brandingDirectory, destinationFileName);

        await using var sourceStream = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var destinationStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);

        CleanupAssets(destinationPath, prefix);
        return destinationPath;
    }

    private void RemoveExistingLogo() => RemoveExistingAsset(_current.LogoPath, "logo");

    private void RemoveExistingIcon() => RemoveExistingAsset(_current.IconPath, "icon");

    private void RemoveExistingAsset(string? path, string prefix)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (IsManagedAsset(path, prefix) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to remove existing {Prefix} file.", prefix);
        }
    }

    private void CleanupAssets(string keepPath, string prefix)
    {
        try
        {
            var directory = new DirectoryInfo(_brandingDirectory);
            if (!directory.Exists)
            {
                return;
            }

            foreach (var file in directory.GetFiles($"{prefix}-*", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(file.FullName, keepPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsManagedAsset(file.FullName, prefix))
                {
                    file.Delete();
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to clean up old {Prefix} files.", prefix);
        }
    }

    private bool IsManagedAsset(string path, string prefix)
    {
        if (!path.StartsWith(_brandingDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fileName = Path.GetFileName(path);
        return fileName.StartsWith($"{prefix}-", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyPalette(BrandingColorScheme scheme)
    {
        if (!PaletteUris.TryGetValue(scheme, out var paletteUri))
        {
            paletteUri = PaletteUris[BrandingColorScheme.Midnight];
        }

        var newPalette = (ResourceDictionary)WpfApplication.LoadComponent(paletteUri);
        var mergedDictionaries = WpfApplication.Current.Resources.MergedDictionaries;

        if (_activePaletteDictionary is not null)
        {
            mergedDictionaries.Remove(_activePaletteDictionary);
        }

        mergedDictionaries.Add(newPalette);
        _activePaletteDictionary = newPalette;
    }

    private sealed record BrandingSettingsDto(string? LogoPath, string? IconPath, string ColorScheme);
}
