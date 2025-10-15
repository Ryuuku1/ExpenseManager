using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.Services.Branding;
using ExpenseManager.Desktop.ViewModels.Abstractions;

namespace ExpenseManager.Desktop.ViewModels;

public sealed class MainWindowViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly IUserInteractionService _interactionService;
    private readonly IUserSessionService _sessionService;
    private readonly IBrandingService _brandingService;
    private readonly ILocalizationManager _localization;
    private readonly ILocalizationService _localizationService;
    private readonly TranslationSource _translationSource = TranslationSource.Instance;
    private readonly Dictionary<string, NavigationDestination> _navigationMap;
    private readonly HashSet<string> _loadedSections = new(StringComparer.OrdinalIgnoreCase);

    public DashboardViewModel Dashboard { get; }

    public string? LogoSource => _brandingService.Current.LogoPath;

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoSource);

    public IAsyncRelayCommand<string?> NavigateCommand { get; }
    public IAsyncRelayCommand<string?> ExecuteQuickActionCommand { get; }

    private object? _currentViewModel;
    private string _currentPageTitle = string.Empty;
    private string _currentPageSubtitle = string.Empty;
    private string _selectedNavigationKey = string.Empty;

    public object CurrentViewModel
    {
        get => _currentViewModel!;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        private set => SetProperty(ref _currentPageTitle, value);
    }

    public string CurrentPageSubtitle
    {
        get => _currentPageSubtitle;
        private set => SetProperty(ref _currentPageSubtitle, value);
    }

    public string SelectedNavigationKey
    {
        get => _selectedNavigationKey;
        private set => SetProperty(ref _selectedNavigationKey, value);
    }

    public MainWindowViewModel(
        DashboardViewModel dashboard,
        ExpensesViewModel expenses,
        CategoriesViewModel categories,
        ReportsViewModel reports,
        CalendarViewModel calendar,
        SettingsViewModel settings,
        IUserInteractionService interactionService,
        IUserSessionService sessionService,
        IBrandingService brandingService,
        ILocalizationManager localization,
        ILocalizationService localizationService)
    {
        Dashboard = dashboard;
        _interactionService = interactionService;
        _sessionService = sessionService;
        _brandingService = brandingService;
        _localization = localization;
        _localizationService = localizationService;

    _translationSource.PropertyChanged += OnTranslationSourceChanged;

        _navigationMap = new Dictionary<string, NavigationDestination>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard"] = new(Translate("NAVIGATION_DASHBOARD"), BuildDashboardSubtitle(), dashboard),
            ["Expenses"] = new(Translate("NAVIGATION_EXPENSES"), Translate("NAVIGATION_EXPENSES_SUBTITLE"), expenses),
            ["Categories"] = new(Translate("NAVIGATION_CATEGORIES"), Translate("NAVIGATION_CATEGORIES_SUBTITLE"), categories),
            ["Reports"] = new(Translate("NAVIGATION_REPORTS"), Translate("NAVIGATION_REPORTS_SUBTITLE"), reports),
            ["Calendar"] = new(Translate("NAVIGATION_CALENDAR"), Translate("NAVIGATION_CALENDAR_SUBTITLE"), calendar),
            ["Settings"] = new(Translate("NAVIGATION_SETTINGS"), Translate("NAVIGATION_SETTINGS_SUBTITLE"), settings)
        };

        _currentViewModel = dashboard;
        CurrentViewModel = dashboard;
        SelectedNavigationKey = "Dashboard";
    CurrentPageTitle = Translate("NAVIGATION_DASHBOARD");
        CurrentPageSubtitle = BuildDashboardSubtitle();

    NavigateCommand = new AsyncRelayCommand<string?>(NavigateAsync);
    ExecuteQuickActionCommand = new AsyncRelayCommand<string?>(ExecuteQuickActionAsync);

    _brandingService.BrandingChanged += OnBrandingChanged;

        if (_sessionService.IsAuthenticated)
        {
            ApplySessionCulture();
            OnUserAuthenticated();
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSectionLoadedAsync(SelectedNavigationKey, cancellationToken);
    }

    public void OnUserAuthenticated()
    {
        ApplySessionCulture();
        UpdateDashboardSubtitle();
        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserHandle));
    }

    public string CurrentUserName => !string.IsNullOrWhiteSpace(_sessionService.DisplayName) ? _sessionService.DisplayName! : Translate("STATUS_SESSION_REQUIRED");

    public string CurrentUserHandle => _sessionService.IsAuthenticated && !string.IsNullOrWhiteSpace(_sessionService.UserName)
        ? Translate("LABEL_USER_WITH_HANDLE", _sessionService.UserName)
        : Translate("ACTION_CLICK_SIGN_IN");

    private async Task NavigateAsync(string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        if (!_navigationMap.TryGetValue(destination, out var target))
        {
            _interactionService.ShowFeatureComingSoon(destination);
            return;
        }

        ApplySessionCulture();
        SelectedNavigationKey = destination;
        CurrentViewModel = target.ViewModel;
        CurrentPageTitle = target.Title;
        CurrentPageSubtitle = target.Subtitle;

        await EnsureSectionLoadedAsync(destination, CancellationToken.None);
    }

    private async Task ExecuteQuickActionAsync(string? actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
        {
            return;
        }

        switch (actionKey)
        {
            case "AddExpense":
                await NavigateAsync("Expenses");
                break;
            case "ViewExpenses":
                await NavigateAsync("Expenses");
                break;
            case "ManageCategories":
                await NavigateAsync("Categories");
                break;
            case "AddCalendarEvent":
                await NavigateAsync("Calendar");
                if (_navigationMap.TryGetValue("Calendar", out var calendarDestination) && calendarDestination.ViewModel is CalendarViewModel calendar)
                {
                    await calendar.AddEventCommand.ExecuteAsync(null);
                }
                break;
            default:
                _interactionService.ShowFeatureComingSoon(actionKey);
                break;
        }
    }

    private async Task EnsureSectionLoadedAsync(string sectionKey, CancellationToken cancellationToken)
    {
        if (!_navigationMap.TryGetValue(sectionKey, out var destination))
        {
            return;
        }

        if (destination.ViewModel is ILoadableViewModel loadable && (!_loadedSections.Contains(sectionKey) || destination.ReloadOnNavigate))
        {
            await loadable.LoadAsync(cancellationToken);
            _loadedSections.Add(sectionKey);
        }
    }

    private sealed record NavigationDestination(string Title, string Subtitle, object ViewModel, bool ReloadOnNavigate = true);

    private string BuildDashboardSubtitle()
    {
        return !string.IsNullOrWhiteSpace(_sessionService.DisplayName)
            ? Translate("GREETING_WELCOME_BACK", _sessionService.DisplayName)
            : Translate("GREETING_WELCOME");
    }

    private void UpdateDashboardSubtitle()
    {
        if (!_navigationMap.TryGetValue("Dashboard", out var destination))
        {
            return;
        }

        var updated = destination with { Subtitle = BuildDashboardSubtitle() };
        _navigationMap["Dashboard"] = updated;

        if (SelectedNavigationKey.Equals("Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            CurrentPageSubtitle = updated.Subtitle;
        }
    }

    private void RefreshNavigationTranslations()
    {
        void UpdateEntry(string key, string title, string subtitle)
        {
            if (_navigationMap.TryGetValue(key, out var destination))
            {
                _navigationMap[key] = destination with { Title = title, Subtitle = subtitle };
            }
        }

        UpdateEntry("Dashboard", Translate("NAVIGATION_DASHBOARD"), BuildDashboardSubtitle());
        UpdateEntry("Expenses", Translate("NAVIGATION_EXPENSES"), Translate("NAVIGATION_EXPENSES_SUBTITLE"));
        UpdateEntry("Categories", Translate("NAVIGATION_CATEGORIES"), Translate("NAVIGATION_CATEGORIES_SUBTITLE"));
        UpdateEntry("Reports", Translate("NAVIGATION_REPORTS"), Translate("NAVIGATION_REPORTS_SUBTITLE"));
        UpdateEntry("Calendar", Translate("NAVIGATION_CALENDAR"), Translate("NAVIGATION_CALENDAR_SUBTITLE"));
        UpdateEntry("Settings", Translate("NAVIGATION_SETTINGS"), Translate("NAVIGATION_SETTINGS_SUBTITLE"));

        if (_navigationMap.TryGetValue(SelectedNavigationKey, out var currentDestination))
        {
            CurrentPageTitle = currentDestination.Title;
            CurrentPageSubtitle = currentDestination.Subtitle;
        }
    }

    private string Translate(string key) => _localization.GetString(key);

    private string Translate(string key, params object[] arguments) => _localization.GetString(key, arguments);

    private void OnBrandingChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(LogoSource));
        OnPropertyChanged(nameof(HasLogo));
    }

    private void OnTranslationSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not null && !string.Equals(e.PropertyName, "Item[]", StringComparison.Ordinal))
        {
            return;
        }

        RefreshNavigationTranslations();

        var refreshed = new HashSet<ILocalizableViewModel>();
        foreach (var destination in _navigationMap.Values)
        {
            if (destination.ViewModel is ILocalizableViewModel localizable && refreshed.Add(localizable))
            {
                localizable.RefreshTranslations();
            }
        }

        if (CurrentViewModel is ILocalizableViewModel current && !refreshed.Contains(current))
        {
            current.RefreshTranslations();
        }

        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserHandle));
    }

    private void ApplySessionCulture()
    {
        var culture = _sessionService.PreferredLanguage;
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        _localizationService.TryApplyCulture(culture, out _);
    }
}
