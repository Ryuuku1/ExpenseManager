using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Application.Infrastructure;
using ExpenseManager.Application.Users.Requests;
using ExpenseManager.Application.Users.Services;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.Services.Branding;
using ExpenseManager.Desktop.ViewModels.Abstractions;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase, ILoadableViewModel, ILocalizableViewModel
{
    private const string DefaultCultureName = "pt-PT";

    private readonly IUserSessionService _sessionService;
    private readonly IUserProfileService _userProfileService;
    private readonly IUserInteractionService _interactionService;
    private readonly IDatabaseBackupService _databaseBackupService;
    private readonly ILocalizationService _localizationService;
    private readonly ILocalizationManager _localization;
    private readonly IBrandingService _brandingService;
    private readonly IFilePickerService _filePickerService;
    private readonly ISupportService _supportService;

    private Guid? _userId;
    private bool _suppressBrandingNotifications;
    private bool _brandingDirty;
    private bool _logoChanged;
    private bool _iconChanged;

    public ObservableCollection<LanguageOptionViewModel> AvailableLanguages { get; } = [];
    public ObservableCollection<BrandingColorSchemeOptionViewModel> AvailableColorSchemes { get; } = [];

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private decimal _monthlyBudget;

    [ObservableProperty]
    private string _preferredLanguage = CultureInfo.CurrentUICulture.Name;

    [ObservableProperty]
    private Currency _preferredCurrency = Currency.Eur;

    [ObservableProperty]
    private string? _logoPath;

    [ObservableProperty]
    private string? _iconPath;

    [ObservableProperty]
    private BrandingColorScheme _selectedColorScheme = BrandingColorScheme.Midnight;

    public SettingsViewModel(
        IUserSessionService sessionService,
        IUserProfileService userProfileService,
        IUserInteractionService interactionService,
        IDatabaseBackupService databaseBackupService,
        ILocalizationService localizationService,
        ILocalizationManager localization,
        IBrandingService brandingService,
        IFilePickerService filePickerService,
        ISupportService supportService)
    {
        _sessionService = sessionService;
        _userProfileService = userProfileService;
        _interactionService = interactionService;
        _databaseBackupService = databaseBackupService;
        _localizationService = localizationService;
        _localization = localization;
        _brandingService = brandingService;
        _filePickerService = filePickerService;
        _supportService = supportService;

        LoadAvailableLanguages();
        LoadAvailableColorSchemes(preserveSelection: true);
    }

    public void RefreshTranslations()
    {
        LoadAvailableColorSchemes(preserveSelection: true);

        OnPropertyChanged(nameof(AvailableColorSchemes));

        var currentScheme = _brandingService.Current.ColorScheme;
        if (SelectedColorScheme != currentScheme)
        {
            var originalSuppression = _suppressBrandingNotifications;
            _suppressBrandingNotifications = true;
            SelectedColorScheme = currentScheme;
            _suppressBrandingNotifications = originalSuppression;
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _userId = _sessionService.UserId;
        if (_userId is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_SETTINGS"), Translate("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        var profile = await _userProfileService.GetProfileAsync(_userId.Value, cancellationToken);
        if (profile is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_SETTINGS"), Translate("ERROR_USER_DATA_LOAD"));
            return;
        }

        Name = profile.Name;
        Email = profile.Email;
        MonthlyBudget = profile.MonthlyBudget;

        var preferredLanguage = NormalizeCulture(profile.PreferredLanguage);
        PreferredLanguage = preferredLanguage;
    _localizationService.TryApplyCulture(preferredLanguage, out _);
    _sessionService.UpdatePreferredLanguage(preferredLanguage);

        PreferredCurrency = profile.PreferredCurrency;

        LoadBrandingState();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _userId = _sessionService.UserId;
        if (_userId is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_SETTINGS"), Translate("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        try
        {
            var normalizedLanguage = NormalizeCulture(PreferredLanguage);
            var request = new UpdateUserProfileRequest(_userId.Value, Name, Email, MonthlyBudget, normalizedLanguage, PreferredCurrency);
            await _userProfileService.UpdateProfileAsync(request, CancellationToken.None);

            if (_brandingDirty)
            {
                var update = new BrandingUpdate(LogoPath, _logoChanged, IconPath, _iconChanged, SelectedColorScheme);
                await _brandingService.UpdateAsync(update, CancellationToken.None);
                LoadBrandingState();
            }

            _interactionService.ShowInformation(Translate("NAVIGATION_SETTINGS"), Translate("INFO_DATA_SAVED"));
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_SETTINGS"), exception.Message);
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = Translate("ACTION_BACKUP"),
            Filter = "Base de Dados SQLite (*.db)|*.db|Todos os ficheiros (*.*)|*.*",
            FileName = $"ExpenseManager-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db"
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        try
        {
            await _databaseBackupService.CreateBackupAsync(dialog.FileName, CancellationToken.None);
            _interactionService.ShowInformation(Translate("ACTION_BACKUP"), Translate("INFO_BACKUP_CREATED"));
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("ACTION_BACKUP"), exception.Message);
        }
    }

    [RelayCommand]
    private void UploadLogo()
    {
        var file = _filePickerService.PickFiles().FirstOrDefault();
        if (file is null)
        {
            return;
        }

        if (!IsSupportedLogo(file.FullPath))
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_SETTINGS"), Translate("MESSAGE_UNSUPPORTED_LOGO_FORMAT"));
            return;
        }

        LogoPath = file.FullPath;
    }

    [RelayCommand]
    private void RemoveLogo()
    {
        if (string.IsNullOrWhiteSpace(LogoPath))
        {
            return;
        }

        LogoPath = null;
    }

    [RelayCommand]
    private void UploadIcon()
    {
        var file = _filePickerService.PickFiles().FirstOrDefault();
        if (file is null)
        {
            return;
        }

        if (!IsSupportedIcon(file.FullPath))
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_SETTINGS"), Translate("MESSAGE_UNSUPPORTED_ICON_FORMAT"));
            return;
        }

        IconPath = file.FullPath;
    }

    [RelayCommand]
    private void RemoveIcon()
    {
        if (string.IsNullOrWhiteSpace(IconPath))
        {
            return;
        }

        IconPath = null;
    }

    [RelayCommand]
    private void SupportDeveloper()
    {
        if (_supportService.TryOpenDonationPage())
        {
            return;
        }

        _interactionService.ShowInformation(Translate("SECTION_SUPPORT"), Translate("ERROR_SUPPORT_LINK"));
    }

    partial void OnPreferredLanguageChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = NormalizeCulture(value);
        if (!string.Equals(normalized, value, StringComparison.OrdinalIgnoreCase))
        {
            PreferredLanguage = normalized;
            return;
        }

        if (!_localizationService.TryApplyCulture(normalized, out var errorMessage))
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                _interactionService.ShowInformation(Translate("NAVIGATION_SETTINGS"), errorMessage);
            }

            return;
        }

        _sessionService.UpdatePreferredLanguage(normalized);
    }

    private string Translate(string text) => _localization.GetString(text);

    private void LoadAvailableLanguages()
    {
        AvailableLanguages.Clear();

        static string ToDisplayName(string cultureCode)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureCode);
                return culture.NativeName;
            }
            catch (CultureNotFoundException)
            {
                return cultureCode;
            }
        }

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DefaultCultureName
        };

        foreach (var culture in _localization.SupportedCultures)
        {
            var normalized = NormalizeCulture(culture.Name);
            codes.Add(normalized);
        }

        foreach (var code in codes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase))
        {
            AvailableLanguages.Add(new LanguageOptionViewModel(code, ToDisplayName(code)));
        }

        if (!AvailableLanguages.Any(option => string.Equals(option.Code, PreferredLanguage, StringComparison.OrdinalIgnoreCase)))
        {
            PreferredLanguage = DefaultCultureName;
        }
    }

    private void LoadAvailableColorSchemes(bool preserveSelection = false)
    {
        var currentSelection = SelectedColorScheme;
        var originalSuppression = _suppressBrandingNotifications;

        AvailableColorSchemes.Clear();

        foreach (var scheme in Enum.GetValues<BrandingColorScheme>())
        {
            AvailableColorSchemes.Add(new BrandingColorSchemeOptionViewModel(scheme, GetColorSchemeDisplayName(scheme)));
        }

        if (preserveSelection)
        {
            _suppressBrandingNotifications = true;
            SelectedColorScheme = currentSelection;
            _suppressBrandingNotifications = originalSuppression;
        }
    }

    private void LoadBrandingState()
    {
        _suppressBrandingNotifications = true;

        var settings = _brandingService.Current;
        LogoPath = settings.LogoPath;
        IconPath = settings.IconPath;
        SelectedColorScheme = settings.ColorScheme;

        _suppressBrandingNotifications = false;
        _brandingDirty = false;
        _logoChanged = false;
        _iconChanged = false;

        OnPropertyChanged(nameof(HasSelectedLogo));
        OnPropertyChanged(nameof(HasSelectedIcon));
    }

    public bool HasSelectedLogo => !string.IsNullOrWhiteSpace(LogoPath);

    public bool HasSelectedIcon => !string.IsNullOrWhiteSpace(IconPath);

    partial void OnLogoPathChanged(string? value)
    {
        _ = value;
        OnPropertyChanged(nameof(HasSelectedLogo));

        if (_suppressBrandingNotifications)
        {
            return;
        }

        _brandingDirty = true;
        _logoChanged = true;
    }

    partial void OnIconPathChanged(string? value)
    {
        _ = value;
        OnPropertyChanged(nameof(HasSelectedIcon));

        if (_suppressBrandingNotifications)
        {
            return;
        }

        _brandingDirty = true;
        _iconChanged = true;
    }

    partial void OnSelectedColorSchemeChanged(BrandingColorScheme value)
    {
        _ = value;
        if (_suppressBrandingNotifications)
        {
            return;
        }

        _brandingDirty = true;
    }

    private static bool IsSupportedLogo(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedIcon(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return extension.Equals(".ico", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return DefaultCultureName;
        }

        try
        {
            var cultureInfo = CultureInfo.GetCultureInfo(culture);
            return cultureInfo.TwoLetterISOLanguageName switch
            {
                "pt" => "pt-PT",
                "en" => "en-US",
                "es" => "es-ES",
                _ => cultureInfo.Name
            };
        }
        catch (CultureNotFoundException)
        {
            return DefaultCultureName;
        }
    }

    public sealed record LanguageOptionViewModel(string Code, string DisplayName);

    public sealed record BrandingColorSchemeOptionViewModel(BrandingColorScheme Scheme, string DisplayName);

    private string GetColorSchemeDisplayName(BrandingColorScheme scheme)
    {
        var key = $"COLOR_SCHEME_{scheme.ToString().ToUpperInvariant()}";
        var translated = _localization.GetString(key);
        return string.Equals(translated, key, StringComparison.Ordinal) ? SplitPascalCase(scheme.ToString()) : translated;
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length * 2);
        builder.Append(value[0]);

        for (var i = 1; i < value.Length; i++)
        {
            var current = value[i];
            var previous = value[i - 1];

            if (char.IsUpper(current) && (char.IsLower(previous) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
