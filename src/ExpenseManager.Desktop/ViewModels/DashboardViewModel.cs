using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Application.Calendar.Models;
using ExpenseManager.Application.Calendar.Requests;
using ExpenseManager.Application.Calendar.Services;
using ExpenseManager.Application.Dashboard.Models;
using ExpenseManager.Application.Dashboard.Requests;
using ExpenseManager.Application.Dashboard.Responses;
using ExpenseManager.Application.Dashboard.Services;
using ExpenseManager.Application.Users.Services;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.ViewModels.Abstractions;
using ExpenseManager.Desktop.ViewModels.Dialogs;
using ExpenseManager.Desktop.Views.Dialogs;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase, ILoadableViewModel, ILocalizableViewModel
{
    private readonly IDashboardService _dashboardService;
    private readonly IUserSessionService _sessionService;
    private readonly ICalendarService _calendarService;
    private readonly IUserInteractionService _interactionService;
    private readonly ILocalizationManager _localization;
    private readonly TranslationSource _translationSource = TranslationSource.Instance;
    private readonly DispatcherTimer _clockTimer;

    private Guid? _userId;
    private Currency _currentCurrency = Currency.Eur;
    private GetDashboardOverviewResponse? _lastOverview;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private double _trendMaximum = 1d;

    [ObservableProperty]
    private string _currentTimeDisplay = string.Empty;

    [ObservableProperty]
    private string _currentDateDisplay = string.Empty;

    public ObservableCollection<SummaryCardItemViewModel> SummaryCards { get; } = new();
    public ObservableCollection<RecentExpenseItemViewModel> RecentExpenses { get; } = new();
    public ObservableCollection<TrendPointViewModel> SpendingTrend { get; } = new();
    public ObservableCollection<AlertItemViewModel> Alerts { get; } = new();
    public ObservableCollection<QuickActionItemViewModel> QuickActions { get; } = new();

    public DashboardViewModel(IDashboardService dashboardService, IUserSessionService sessionService, ICalendarService calendarService, IUserInteractionService interactionService, ILocalizationManager localization)
    {
        _dashboardService = dashboardService;
        _sessionService = sessionService;
        _calendarService = calendarService;
        _interactionService = interactionService;
        _localization = localization;

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += OnClockTick;
        UpdateClock(DateTime.Now);
        _clockTimer.Start();

        _translationSource.PropertyChanged += OnTranslationSourceChanged;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var userId = _sessionService.UserId;
            if (userId is null)
            {
                ErrorMessage = TranslateKey("ERROR_NO_USER_CONFIGURED");
                return;
            }

            _userId = userId;

            var month = DateOnly.FromDateTime(DateTime.Today);
            var response = await _dashboardService.GetOverviewAsync(new GetDashboardOverviewRequest(userId.Value, month), cancellationToken);
            ApplyOverview(response);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void RefreshTranslations()
    {
        if (_lastOverview is null)
        {
            return;
        }

        ApplyOverview(_lastOverview);
    }

    private void ApplyOverview(GetDashboardOverviewResponse response)
    {
        _lastOverview = response;

        PopulateSummaryCards(response.Summary);
        PopulateRecentExpenses(response.RecentExpenses);
        PopulateTrend(response.SpendingTrend);
        PopulateAlerts(response.Alerts);
        PopulateQuickActions(response.QuickActions);
    }

    private void PopulateSummaryCards(DashboardSummary summary)
    {
        SummaryCards.Clear();

        _currentCurrency = summary.Currency;
        var culture = CultureInfo.CurrentUICulture;
        var expensesValue = FormatCurrency(summary.CurrentMonthExpenses, summary.Currency);
        var remainingValue = FormatCurrency(summary.RemainingBudget, summary.Currency);

        SummaryCards.Add(new SummaryCardItemViewModel(
            _localization.GetString("DASHBOARD_CARD_EXPENSES_THIS_MONTH"),
            expensesValue,
            "\uE8C7",
            "info"));

        SummaryCards.Add(new SummaryCardItemViewModel(
            _localization.GetString("DASHBOARD_CARD_REMAINING_BUDGET"),
            remainingValue,
            "\uE825",
            "success"));

        SummaryCards.Add(new SummaryCardItemViewModel(
            _localization.GetString("DASHBOARD_CARD_CATEGORIES_USED"),
            summary.CategoriesUsed.ToString(culture),
            "\uE8EF",
            "neutral"));

        SummaryCards.Add(new SummaryCardItemViewModel(
            _localization.GetString("DASHBOARD_CARD_PENDING_ALERTS"),
            summary.PendingAlerts.ToString(culture),
            "\uE814",
            summary.PendingAlerts > 0 ? "danger" : "neutral"));
    }

    private void PopulateRecentExpenses(IReadOnlyCollection<DashboardExpenseItem> expenses)
    {
        RecentExpenses.Clear();
        var culture = CultureInfo.CurrentUICulture;
        foreach (var expense in expenses)
        {
            var amountText = FormatCurrency(expense.Amount, expense.Currency);
            var categoryDisplay = CategoryLocalization.TranslateName(_localization, expense.CategoryName);
            RecentExpenses.Add(new RecentExpenseItemViewModel(
                expense.ExpenseId,
                expense.Title,
                categoryDisplay,
                amountText,
                expense.ExpenseDate.ToString("dd MMM", culture)));
        }
    }

    private void PopulateTrend(IReadOnlyCollection<DashboardTrendPoint> points)
    {
        SpendingTrend.Clear();
        foreach (var point in points)
        {
            var amountText = FormatCurrency(point.Amount, _currentCurrency);
            var label = FormatTrendLabel(point.Date);
            SpendingTrend.Add(new TrendPointViewModel(label, amountText, Convert.ToDouble(point.Amount)));
        }

        TrendMaximum = SpendingTrend.Count > 0
            ? Math.Max(1d, SpendingTrend.Max(item => item.RawAmount))
            : 1d;
    }

    private void PopulateAlerts(IReadOnlyCollection<DashboardAlertItem> alerts)
    {
        Alerts.Clear();
        foreach (var alert in alerts)
        {
            var alertTypeDisplay = AlertLocalization.TranslateType(_localization, alert.AlertType);
            Alerts.Add(new AlertItemViewModel(alert.EventId, alert.Title, alert.OccursAt, alert.AlertType, alert.IsRecurring, alert.IsDismissed, alertTypeDisplay));
        }
    }

    private void PopulateQuickActions(IReadOnlyCollection<DashboardQuickAction> actions)
    {
        QuickActions.Clear();
        foreach (var action in actions)
        {
            var label = _localization.GetString(action.LabelKey);
            QuickActions.Add(new QuickActionItemViewModel(label, action.IconGlyph, action.CommandKey));
        }
    }

    [RelayCommand]
    private async Task EditAlertAsync(AlertItemViewModel? alert)
    {
        if (alert is null)
        {
            return;
        }

        _userId ??= _sessionService.UserId;
        if (_userId is null)
        {
            _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        var calendarEvent = await LoadEventAsync(_userId.Value, alert.EventId, alert.OccursAt, CancellationToken.None);
        if (calendarEvent is null)
        {
            _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("ERROR_ALERT_UPDATE"));
            return;
        }

        var dialogViewModel = new AlertEditorDialogViewModel();
        dialogViewModel.Load(calendarEvent, alert.OccursAt);

        var dialog = new AlertEditorDialog(dialogViewModel)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        try
        {
            var request = dialog.ViewModel.ToRequest(_userId.Value);
            var updated = await _calendarService.UpdateEventAsync(request, CancellationToken.None);
            if (!updated)
            {
                _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("ERROR_ALERT_UPDATE"));
                return;
            }

            await LoadAsync(CancellationToken.None);
            _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("INFO_ALERT_UPDATED"));
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), exception.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAlertAsync(AlertItemViewModel? alert)
    {
        if (alert is null)
        {
            return;
        }

        _userId ??= _sessionService.UserId;
        if (_userId is null)
        {
            _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        try
        {
            if (alert.IsRecurring)
            {
                var choice = _interactionService.ConfirmRecurringDeletion(TranslateKey("SECTION_ALERTS"), TranslateKey("CONFIRM_RECURRING_EVENT_DELETE"));
                switch (choice)
                {
                    case RecurringEventDeletionChoice.Cancel:
                        return;
                    case RecurringEventDeletionChoice.SingleOccurrence:
                    {
                        var dismissRequest = new DismissCalendarEventRequest(_userId.Value, alert.EventId, alert.OccursAt);
                        var dismissed = await _calendarService.DismissOccurrenceAsync(dismissRequest, CancellationToken.None);
                        if (!dismissed)
                        {
                            _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("ERROR_ALERT_REMOVE"));
                            return;
                        }

                        await LoadAsync(CancellationToken.None);
                        _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("INFO_ALERT_DISMISSED"));
                        return;
                    }
                    case RecurringEventDeletionChoice.EntireSeries:
                    {
                        var confirmationMessage = string.Format(CultureInfo.CurrentCulture, TranslateKey("CONFIRM_EVENT_DELETE"), alert.Title);
                        if (!_interactionService.Confirm(TranslateKey("SECTION_ALERTS"), confirmationMessage))
                        {
                            return;
                        }

                        break;
                    }
                }
            }
            else
            {
                if (!_interactionService.Confirm(TranslateKey("SECTION_ALERTS"), TranslateKey("CONFIRM_ALERT_REMOVE")))
                {
                    return;
                }
            }

            var removed = await _calendarService.DeleteEventAsync(_userId.Value, alert.EventId, CancellationToken.None);
            if (!removed)
            {
                _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("ERROR_ALERT_REMOVE"));
                return;
            }

            await LoadAsync(CancellationToken.None);
            _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), TranslateKey("INFO_ALERT_REMOVED"));
        }
        catch (Exception exception)
        {
            var message = string.Format(CultureInfo.CurrentCulture, TranslateKey("ERROR_ALERT_REMOVE_WITH_DESCRIPTION"), exception.Message);
            _interactionService.ShowInformation(TranslateKey("SECTION_ALERTS"), message);
        }
    }

    private string FormatTrendLabel(DateOnly date)
    {
        var dayValue = date.Day.ToString(CultureInfo.CurrentUICulture);
        var monthText = GetShortMonthName(date.Month);
        var format = TranslateKey("DASHBOARD_SPENDING_TREND_LABEL_FORMAT");

        if (string.Equals(format, "DASHBOARD_SPENDING_TREND_LABEL_FORMAT", StringComparison.Ordinal))
        {
            format = "{0} {1}";
        }

        return string.Format(CultureInfo.CurrentUICulture, format, dayValue, monthText);
    }

    private string GetShortMonthName(int month)
    {
        var monthKey = $"MONTH_SHORT_{month:D2}";
        var monthText = TranslateKey(monthKey);

        if (string.Equals(monthText, monthKey, StringComparison.Ordinal))
        {
            var sampleDate = new DateOnly(2000, month, 1);
            monthText = sampleDate.ToDateTime(TimeOnly.MinValue).ToString("MMM", CultureInfo.CurrentUICulture);
        }

        return monthText;
    }

    private async Task<CalendarEventItem?> LoadEventAsync(Guid userId, Guid eventId, DateTime occurrenceUtc, CancellationToken cancellationToken)
    {
        var fromUtc = occurrenceUtc.AddYears(-1);
        var toUtc = occurrenceUtc.AddYears(1);

        if (fromUtc > toUtc)
        {
            (fromUtc, toUtc) = (toUtc, fromUtc);
        }

        var events = await _calendarService.GetUpcomingEventsAsync(userId, fromUtc, toUtc, cancellationToken);
        return events.FirstOrDefault(item => item.Id == eventId);
    }

    private string TranslateKey(string key) => _localization.GetString(key);

    private static string FormatCurrency(decimal amount, Currency currency)
    {
        var culture = CultureInfo.CurrentUICulture;
        var format = (NumberFormatInfo)culture.NumberFormat.Clone();
        format.CurrencySymbol = currency switch
        {
            Currency.Eur => "€",
            Currency.Gbp => "£",
            Currency.Brl => "R$",
            _ => "$"
        };

        return amount.ToString("C", format);
    }

    private void OnClockTick(object? sender, EventArgs e) => UpdateClock(DateTime.Now);

    private void UpdateClock(DateTime timestamp)
    {
        var culture = CultureInfo.CurrentUICulture;
        CurrentTimeDisplay = timestamp.ToString("T", culture);
        CurrentDateDisplay = timestamp.ToString("D", culture);
    }

    private void OnTranslationSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || string.Equals(e.PropertyName, "Item[]", StringComparison.Ordinal))
        {
            RefreshTranslations();
        }
    }
}
