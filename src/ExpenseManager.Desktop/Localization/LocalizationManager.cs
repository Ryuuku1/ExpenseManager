using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseManager.Desktop.Localization;

internal sealed class LocalizationManager : ILocalizationManager
{
    private readonly string _resourcesRoot;
    private readonly LocalizationOptions _options;
    private readonly ILogger<LocalizationManager>? _logger;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cultureLock = new();

    public LocalizationManager(IOptions<LocalizationOptions>? options, ILogger<LocalizationManager>? logger = null)
    {
        _options = options?.Value ?? new LocalizationOptions();
        _logger = logger;

        _resourcesRoot = Path.Combine(AppContext.BaseDirectory, "Resources", "Localization");

        SupportedCultures = DiscoverSupportedCultures();
        DefaultCulture = ResolveCulture(_options.DefaultCulture) ?? CultureInfo.GetCultureInfo("pt-PT");
        FallbackCulture = ResolveCulture(_options.FallbackCulture) ?? DefaultCulture;
        CurrentCulture = DefaultCulture;

        ApplyThreadCulture(DefaultCulture);
        ValidateResourceCoverage();
    }

    private CultureInfo FallbackCulture { get; }

    public CultureInfo CurrentCulture { get; private set; }

    public CultureInfo DefaultCulture { get; }

    public IReadOnlyList<CultureInfo> SupportedCultures { get; }

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    public bool TrySetCulture(string cultureName, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            errorMessage = GetString("ERROR_LANGUAGE_INVALID");
            return false;
        }

        CultureInfo requested;
        try
        {
            requested = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            var format = GetString("ERROR_LANGUAGE_UNSUPPORTED");
            errorMessage = string.Format(CultureInfo.CurrentCulture, format, cultureName);
            return false;
        }

        var resolved = ResolveSupportedCulture(requested) ?? FallbackCulture;
        if (resolved is null)
        {
            errorMessage = GetString("ERROR_LANGUAGE_UNSUPPORTED");
            return false;
        }

        lock (_cultureLock)
        {
            if (CultureEquals(CurrentCulture, resolved))
            {
                errorMessage = null;
                return true;
            }

            var previous = CurrentCulture;
            CurrentCulture = resolved;
            ApplyThreadCulture(resolved);
            errorMessage = null;
            _cache.Clear();
            CultureChanged?.Invoke(this, new CultureChangedEventArgs(previous, resolved));
            return true;
        }
    }

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var culture = CurrentCulture;
        return Lookup(key, culture)
               ?? Lookup(key, culture.Parent)
               ?? Lookup(key, FallbackCulture)
               ?? key;
    }

    public string GetString(string key, params object[] arguments)
    {
        var format = GetString(key);
        if (arguments is null || arguments.Length == 0)
        {
            return format;
        }

        try
        {
            return string.Format(CurrentCulture, format, arguments);
        }
        catch (FormatException exception)
        {
            _logger?.LogWarning(exception, "Failed to format localized string '{Key}'", key);
            return format;
        }
    }

    private string? Lookup(string key, CultureInfo culture)
    {
        if (culture.Equals(CultureInfo.InvariantCulture))
        {
            return null;
        }

        var resources = LoadResources(culture);
        if (resources.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }

    private IReadOnlyDictionary<string, string> LoadResources(CultureInfo culture)
    {
        var cacheKey = culture.Name;
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cultureDirectory = Path.Combine(_resourcesRoot, culture.Name);

        if (Directory.Exists(cultureDirectory))
        {
            foreach (var filePath in Directory.EnumerateFiles(cultureDirectory, "*.json", SearchOption.AllDirectories))
            {
                MergeJsonFile(map, filePath);
            }
        }

        var cultureFile = Path.Combine(_resourcesRoot, $"{culture.Name}.json");
        if (File.Exists(cultureFile))
        {
            MergeJsonFile(map, cultureFile);
        }

        _cache[cacheKey] = map;
        return map;
    }

    private void MergeJsonFile(IDictionary<string, string> target, string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                FlattenJson(property, property.Name, target);
            }
        }
        catch (Exception exception)
        {
            _logger?.LogWarning(exception, "Failed to merge localization file '{FilePath}'", filePath);
        }
    }

    private void FlattenJson(JsonProperty property, string key, IDictionary<string, string> target)
    {
        switch (property.Value.ValueKind)
        {
            case JsonValueKind.String:
                target[key] = property.Value.GetString() ?? string.Empty;
                break;
            case JsonValueKind.Object:
                foreach (var child in property.Value.EnumerateObject())
                {
                    var childKey = string.Create(key.Length + child.Name.Length + 1, (key, child.Name), static (span, state) =>
                    {
                        var (parent, name) = state;
                        parent.AsSpan().CopyTo(span);
                        span[parent.Length] = '.';
                        name.AsSpan().CopyTo(span[(parent.Length + 1)..]);
                    });
                    FlattenJson(child, childKey, target);
                }
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                target[key] = property.Value.GetRawText();
                break;
            default:
                target[key] = property.Value.ToString();
                break;
        }
    }

    private IReadOnlyList<CultureInfo> DiscoverSupportedCultures()
    {
        var cultures = new SortedDictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo(name);
                cultures[name] = culture;
            }
            catch (CultureNotFoundException)
            {
                _logger?.LogWarning("Configured culture '{Culture}' is not valid and will be ignored.", name);
            }
        }

        if (Directory.Exists(_resourcesRoot))
        {
            foreach (var directory in Directory.EnumerateDirectories(_resourcesRoot))
            {
                var cultureName = Path.GetFileName(directory);
                TryAdd(cultureName);
            }

            foreach (var file in Directory.EnumerateFiles(_resourcesRoot, "*.json", SearchOption.TopDirectoryOnly))
            {
                var cultureName = Path.GetFileNameWithoutExtension(file);
                TryAdd(cultureName);
            }
        }

        if (_options.SupportedCultures is { Length: > 0 })
        {
            foreach (var cultureName in _options.SupportedCultures)
            {
                TryAdd(cultureName);
            }
        }

        TryAdd(_options.DefaultCulture);
        TryAdd(_options.FallbackCulture);

        if (cultures.Count == 0)
        {
            var fallback = CultureInfo.GetCultureInfo("pt-PT");
            cultures[fallback.Name] = fallback;
        }

        return cultures.Values.ToList();
    }

    private CultureInfo? ResolveSupportedCulture(CultureInfo culture)
    {
        if (SupportedCultures.Count == 0)
        {
            return null;
        }

        if (SupportedCultures.FirstOrDefault(c => CultureEquals(c, culture)) is { } direct)
        {
            return direct;
        }

        var parent = culture.Parent;
        if (!parent.Equals(CultureInfo.InvariantCulture))
        {
            var resolvedParent = SupportedCultures.FirstOrDefault(c => CultureEquals(c, parent));
            if (resolvedParent is not null)
            {
                return resolvedParent;
            }
        }

        return SupportedCultures.FirstOrDefault(c => CultureEquals(c, FallbackCulture))
               ?? SupportedCultures.First();
    }

    private CultureInfo? ResolveCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            _logger?.LogWarning("Fallback culture '{Culture}' is not valid.", cultureName);
            return null;
        }
    }

    private static bool CultureEquals(CultureInfo first, CultureInfo second)
    {
        return string.Equals(first.Name, second.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyThreadCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private void ValidateResourceCoverage()
    {
        if (SupportedCultures.Count == 0)
        {
            return;
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        var allKeys = new HashSet<string>(comparer);
        var cultureResources = new Dictionary<CultureInfo, IReadOnlyDictionary<string, string>>();

        foreach (var culture in SupportedCultures)
        {
            var resources = LoadResources(culture);
            cultureResources[culture] = resources;
            foreach (var key in resources.Keys)
            {
                allKeys.Add(key);
            }
        }

        if (allKeys.Count == 0)
        {
            _logger?.LogWarning("No localization keys discovered under '{ResourcesRoot}'.", _resourcesRoot);
            return;
        }

        foreach (var (culture, resources) in cultureResources)
        {
            if (resources.Count == 0)
            {
                _logger?.LogWarning("Localization culture '{Culture}' does not define any keys.", culture.Name);
                continue;
            }

            var missingKeys = allKeys.Where(key => !resources.ContainsKey(key)).ToList();
            if (missingKeys.Count == 0)
            {
                continue;
            }

            var preview = string.Join(", ", missingKeys.Take(10));
            _logger?.LogWarning(
                "Localization culture '{Culture}' is missing {MissingCount} key(s). Missing examples: {MissingKeys}",
                culture.Name,
                missingKeys.Count,
                preview);
        }
    }
}
