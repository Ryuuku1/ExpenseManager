using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Application.Calendar.Models;
using ExpenseManager.Application.Calendar.Requests;
using ExpenseManager.Application.Calendar.Services;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.ViewModels.Abstractions;
using ExpenseManager.Desktop.ViewModels.Dialogs;
using ExpenseManager.Desktop.ViewModels.Items;
using ExpenseManager.Desktop.Views.Dialogs;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels;

public sealed partial class CalendarViewModel : ViewModelBase, ILoadableViewModel, ILocalizableViewModel
{
	private readonly ICalendarService _calendarService;
	private readonly IUserSessionService _sessionService;
	private readonly IUserInteractionService _interactionService;
	private readonly ILocalizationManager _localization;

	private void OnTranslationSourceChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is null || string.Equals(e.PropertyName, "Item[]", StringComparison.Ordinal))
		{
			RefreshTranslations();
		}
	}
	private readonly TranslationSource _translationSource = TranslationSource.Instance;
	private readonly List<CalendarEventItem> _sourceEvents = new();
	private readonly List<CalendarEventViewModel> _allEvents = new();

	private Guid? _userId;
	private bool _suppressAutoRefresh;
	private DateTime _rangeStartUtc;
	private DateTime _rangeEndUtc;

	public ObservableCollection<CalendarEventViewModel> Events { get; } = new();
	public ObservableCollection<CalendarRangeOptionViewModel> RangeOptions { get; } = new();

	public Array AlertTypes { get; } = Enum.GetValues(typeof(AlertType));
	public Array RecurrenceTypes { get; } = Enum.GetValues(typeof(RecurrenceType));

	[ObservableProperty]
	private DateTime _focusDate = DateTime.Today;

	[ObservableProperty]
	private CalendarRangeOptionViewModel? _selectedRange;

	[ObservableProperty]
	private string? _searchQuery;

	// Inline add event form state
	[ObservableProperty]
	private bool _isAddFormVisible;

	[ObservableProperty]
	private string _newEventTitle = string.Empty;

	[ObservableProperty]
	private string? _newEventNotes;

	[ObservableProperty]
	private DateTime _newEventDate = DateTime.Today;

	[ObservableProperty]
	private string _newEventTime = DateTime.Now.ToString("HH:mm");

	[ObservableProperty]
	private bool _newEventUseReminder;

	[ObservableProperty]
	private int _newEventReminderMinutes = 30;

	[ObservableProperty]
	private AlertType _newEventAlertType = AlertType.Custom;

	[ObservableProperty]
	private RecurrenceType _newEventRecurrence = RecurrenceType.None;

	public CalendarViewModel(ICalendarService calendarService, IUserSessionService sessionService, IUserInteractionService interactionService, ILocalizationManager localization)
	{
		_calendarService = calendarService;
		_sessionService = sessionService;
		_interactionService = interactionService;
		_localization = localization;

		_suppressAutoRefresh = true;
		InitializeRangeOptions();
		FocusDate = DateTime.Today;
		SelectedRange = RangeOptions.FirstOrDefault(option => option.Kind == CalendarRangeKind.Month);
		_suppressAutoRefresh = false;

		_translationSource.PropertyChanged += OnTranslationSourceChanged;
	}

	[RelayCommand]
	private async Task AddEventAsync()
	{
		_userId ??= _sessionService.UserId;
		if (_userId is null)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_NO_USER_CONFIGURED"));
			return;
		}

		var dialogViewModel = new CalendarEventEditorDialogViewModel();
		var dialog = new CalendarEventEditorDialog(dialogViewModel)
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
			await _calendarService.CreateEventAsync(request, CancellationToken.None);
			await ReloadAsync(CancellationToken.None);
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("INFO_ALERT_CREATED"));
		}
		catch (Exception exception)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), exception.Message);
		}
	}

	public async Task LoadAsync(CancellationToken cancellationToken = default)
	{
		_userId ??= _sessionService.UserId;
		if (_userId is null)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_NO_USER_CONFIGURED"));
			return;
		}

		await ReloadAsync(cancellationToken);
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		await ReloadAsync(CancellationToken.None);
	}

	[RelayCommand]
	private async Task DeleteEventAsync(CalendarEventViewModel? calendarEvent)
	{
		if (calendarEvent is null)
		{
			return;
		}

		_userId ??= _sessionService.UserId;
		if (_userId is null)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_NO_USER_CONFIGURED"));
			return;
		}

		try
		{
			if (calendarEvent.Recurrence != RecurrenceType.None)
			{
				var choice = _interactionService.ConfirmRecurringDeletion(Translate("NAVIGATION_CALENDAR"), Translate("CONFIRM_RECURRING_EVENT_DELETE"));
				switch (choice)
				{
					case RecurringEventDeletionChoice.Cancel:
						return;
					case RecurringEventDeletionChoice.SingleOccurrence:
					{
						var dismissRequest = new DismissCalendarEventRequest(_userId.Value, calendarEvent.Id, calendarEvent.ScheduledAt);
						var dismissed = await _calendarService.DismissOccurrenceAsync(dismissRequest, CancellationToken.None);
						if (!dismissed)
						{
							_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_ALERT_REMOVE"));
							return;
						}

						await ReloadAsync(CancellationToken.None);
						_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("INFO_ALERT_DISMISSED"));
						return;
					}
					case RecurringEventDeletionChoice.EntireSeries:
						break;
				}

				var confirmationMessage = string.Format(CultureInfo.CurrentCulture, Translate("CONFIRM_EVENT_DELETE"), calendarEvent.Title);
				if (!_interactionService.Confirm(Translate("NAVIGATION_CALENDAR"), confirmationMessage))
				{
					return;
				}
			}

			var removed = await _calendarService.DeleteEventAsync(_userId.Value, calendarEvent.Id, CancellationToken.None);
			if (!removed)
			{
				_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_ALERT_REMOVE"));
				return;
			}

			await ReloadAsync(CancellationToken.None);
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("INFO_ALERT_REMOVED"));
		}
		catch (Exception exception)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), exception.Message);
		}
	}

	[RelayCommand]
	private async Task DismissEventAsync(CalendarEventViewModel? calendarEvent)
	{
		if (calendarEvent is null)
		{
			return;
		}

		_userId ??= _sessionService.UserId;
		if (_userId is null)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_NO_USER_CONFIGURED"));
			return;
		}

		try
		{
			var request = new DismissCalendarEventRequest(_userId.Value, calendarEvent.Id, calendarEvent.ScheduledAt);
			var dismissed = await _calendarService.DismissOccurrenceAsync(request, CancellationToken.None);
			if (!dismissed)
			{
				_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_ALERT_ACK"));
				return;
			}

			await ReloadAsync(CancellationToken.None);
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("INFO_ALERT_DISMISSED"));
		}
		catch (Exception exception)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), exception.Message);
		}
	}

	[RelayCommand]
	private async Task EditEventAsync(CalendarEventViewModel? calendarEvent)
	{
		if (calendarEvent is null)
		{
			return;
		}

		_userId ??= _sessionService.UserId;
		if (_userId is null)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_NO_USER_CONFIGURED"));
			return;
		}

		var eventItem = _sourceEvents.FirstOrDefault(item => item.Id == calendarEvent.Id && item.ScheduledAt == calendarEvent.ScheduledAt);
		if (eventItem is null)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_ALERT_UPDATE"));
			return;
		}

		var dialogViewModel = new AlertEditorDialogViewModel();
		dialogViewModel.Load(eventItem, calendarEvent.ScheduledAt);

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
				_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_ALERT_UPDATE"));
				return;
			}

			await ReloadAsync(CancellationToken.None);
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("INFO_ALERT_UPDATED"));
		}
		catch (Exception exception)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), exception.Message);
		}
	}

	private void InitializeRangeOptions()
	{
		RangeOptions.Clear();
		RangeOptions.Add(CreateRangeOption(CalendarRangeKind.Day, "CALENDAR_RANGE_DAY"));
		RangeOptions.Add(CreateRangeOption(CalendarRangeKind.Week, "CALENDAR_RANGE_WEEK"));
		RangeOptions.Add(CreateRangeOption(CalendarRangeKind.Month, "CALENDAR_RANGE_MONTH"));
		RangeOptions.Add(CreateRangeOption(CalendarRangeKind.Quarter, "CALENDAR_RANGE_QUARTER"));
		RangeOptions.Add(CreateRangeOption(CalendarRangeKind.Year, "CALENDAR_RANGE_YEAR"));
	}

	private CalendarRangeOptionViewModel CreateRangeOption(CalendarRangeKind kind, string translationKey)
	{
		return new CalendarRangeOptionViewModel(kind, Translate(translationKey));
	}

	private async Task ReloadAsync(CancellationToken cancellationToken)
	{
		if (_userId is null)
		{
			return;
		}

		var (fromUtc, toUtc) = GetCurrentRangeBounds();
		_rangeStartUtc = fromUtc;
		_rangeEndUtc = toUtc;

		var events = await _calendarService.GetUpcomingEventsAsync(_userId.Value, fromUtc, toUtc, cancellationToken);
		_sourceEvents.Clear();
		_sourceEvents.AddRange(events);
		RebuildEvents();

		ApplyFilters();
	}

	public void RefreshTranslations()
	{
		var selectedKind = SelectedRange?.Kind ?? CalendarRangeKind.Month;
		_suppressAutoRefresh = true;
		InitializeRangeOptions();
		SelectedRange = RangeOptions.FirstOrDefault(option => option.Kind == selectedKind);
		_suppressAutoRefresh = false;

		RebuildEvents();
		ApplyFilters();
	}

	private void RebuildEvents()
	{
		_allEvents.Clear();
		foreach (var calendarEvent in _sourceEvents)
		{
			var recurrenceDisplay = TranslateRecurrence(calendarEvent.Recurrence);
			var reminderDisplay = BuildReminderDisplay(calendarEvent.ReminderOffset);
			var eventTypeDisplay = TranslateAlertType(calendarEvent.EventType);

			_allEvents.Add(new CalendarEventViewModel(
				calendarEvent.Id,
				calendarEvent.Title,
				calendarEvent.Notes,
				calendarEvent.ScheduledAt,
				calendarEvent.ReminderOffset,
				calendarEvent.EventType,
				calendarEvent.Recurrence,
				calendarEvent.LinkedExpenseId,
				calendarEvent.DismissedUntilUtc,
				recurrenceDisplay,
				reminderDisplay,
				eventTypeDisplay));
		}
	}

	private void ApplyFilters()
	{
		var search = string.IsNullOrWhiteSpace(SearchQuery)
			? null
			: SearchQuery.Trim();

		IEnumerable<CalendarEventViewModel> filteredEvents = _allEvents
			.Where(calendarEvent => calendarEvent.ScheduledAt >= _rangeStartUtc && calendarEvent.ScheduledAt <= _rangeEndUtc);

		if (!string.IsNullOrEmpty(search))
		{
			filteredEvents = filteredEvents.Where(calendarEvent =>
				calendarEvent.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
				(!string.IsNullOrEmpty(calendarEvent.Notes) && calendarEvent.Notes.Contains(search, StringComparison.CurrentCultureIgnoreCase)) ||
				calendarEvent.EventTypeDisplay.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
				calendarEvent.ReminderDisplay.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
				calendarEvent.RecurrenceDisplay.Contains(search, StringComparison.CurrentCultureIgnoreCase));
		}

		filteredEvents = filteredEvents
			.OrderBy(calendarEvent => calendarEvent.ScheduledAt)
			.ThenBy(calendarEvent => calendarEvent.Title);

		ReplaceItems(Events, filteredEvents);
	}

	private (DateTime fromUtc, DateTime toUtc) GetCurrentRangeBounds()
	{
		var selectedKind = SelectedRange?.Kind ?? CalendarRangeKind.Month;
		var focusLocalDate = DateTime.SpecifyKind(FocusDate.Date, DateTimeKind.Local);

		DateTime startLocal = selectedKind switch
		{
			CalendarRangeKind.Day => focusLocalDate,
			CalendarRangeKind.Week => GetWeekStart(focusLocalDate),
			CalendarRangeKind.Month => GetMonthStart(focusLocalDate),
			CalendarRangeKind.Quarter => GetQuarterStart(focusLocalDate),
			CalendarRangeKind.Year => GetYearStart(focusLocalDate),
			_ => focusLocalDate
		};

		DateTime endLocal = selectedKind switch
		{
			CalendarRangeKind.Day => startLocal.AddDays(1).AddTicks(-1),
			CalendarRangeKind.Week => startLocal.AddDays(7).AddTicks(-1),
			CalendarRangeKind.Month => startLocal.AddMonths(1).AddTicks(-1),
			CalendarRangeKind.Quarter => startLocal.AddMonths(3).AddTicks(-1),
			CalendarRangeKind.Year => startLocal.AddYears(1).AddTicks(-1),
			_ => startLocal.AddDays(1).AddTicks(-1)
		};

		var fromUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Local).ToUniversalTime();
		var toUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Local).ToUniversalTime();

		return (fromUtc, toUtc);
	}

	private static DateTime GetWeekStart(DateTime focusLocalDate)
	{
		var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
		var offset = (7 + (focusLocalDate.DayOfWeek - firstDayOfWeek)) % 7;
		return focusLocalDate.AddDays(-offset);
	}

	private static DateTime GetMonthStart(DateTime focusLocalDate)
	{
		return new DateTime(focusLocalDate.Year, focusLocalDate.Month, 1, 0, 0, 0, DateTimeKind.Local);
	}

	private static DateTime GetQuarterStart(DateTime focusLocalDate)
	{
		var quarterIndex = (focusLocalDate.Month - 1) / 3;
		var startMonth = quarterIndex * 3 + 1;
		return new DateTime(focusLocalDate.Year, startMonth, 1, 0, 0, 0, DateTimeKind.Local);
	}

	private static DateTime GetYearStart(DateTime focusLocalDate)
	{
		return new DateTime(focusLocalDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);
	}

	private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> items)
	{
		target.Clear();
		foreach (var item in items)
		{
			target.Add(item);
		}
	}

	private string Translate(string text) => _localization.GetString(text);

	private string TranslateRecurrence(RecurrenceType recurrence) => recurrence switch
	{
		RecurrenceType.None => Translate("RECURRENCE_NONE"),
		RecurrenceType.Daily => Translate("RECURRENCE_DAILY"),
		RecurrenceType.Weekly => Translate("RECURRENCE_WEEKLY"),
		RecurrenceType.Monthly => Translate("RECURRENCE_MONTHLY"),
		RecurrenceType.Quarterly => Translate("RECURRENCE_QUARTERLY"),
		RecurrenceType.Yearly => Translate("RECURRENCE_ANNUAL"),
		_ => Translate(recurrence.ToString())
	};

	private string BuildReminderDisplay(TimeSpan? reminderOffset)
	{
		if (reminderOffset is null)
		{
			return Translate("CALENDAR_REMINDER_NONE");
		}

		var formatted = reminderOffset.Value.ToString(@"hh\\:mm", CultureInfo.CurrentCulture);
		return string.Format(CultureInfo.CurrentCulture, Translate("CALENDAR_REMINDER_WITH_OFFSET"), formatted);
	}

	private string TranslateAlertType(AlertType alertType) => AlertLocalization.TranslateType(_localization, alertType);

	partial void OnFocusDateChanged(DateTime value)
	{
		_ = value;
		TriggerRangeRefresh();
	}

	partial void OnSelectedRangeChanged(CalendarRangeOptionViewModel? value)
	{
		if (value is null)
		{
			return;
		}

		TriggerRangeRefresh();
	}

	partial void OnSearchQueryChanged(string? value)
	{
		_ = value;
		if (_suppressAutoRefresh)
		{
			return;
		}

		ApplyFilters();
	}

	private void TriggerRangeRefresh()
	{
		if (_suppressAutoRefresh)
		{
			return;
		}

		_ = RefreshCommand.ExecuteAsync(null);
	}

	[RelayCommand]
	private void ShowAddEvent()
	{
		IsAddFormVisible = true;
	}

	[RelayCommand]
	private void CancelNewEvent()
	{
		IsAddFormVisible = false;
		NewEventTitle = string.Empty;
		NewEventNotes = null;
		NewEventDate = DateTime.Today;
		NewEventTime = DateTime.Now.ToString("HH:mm");
		NewEventUseReminder = false;
		NewEventReminderMinutes = 30;
		NewEventAlertType = AlertType.Custom;
		NewEventRecurrence = RecurrenceType.None;
	}

	[RelayCommand]
	private async Task SaveNewEventAsync()
	{
		_userId ??= _sessionService.UserId;
		if (_userId is null)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_NO_USER_CONFIGURED"));
			return;
		}

		if (string.IsNullOrWhiteSpace(NewEventTitle))
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_ALERT_TITLE_REQUIRED"));
			return;
		}

		if (!TryParseTime(NewEventTime, out var hours, out var minutes))
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_ALERT_TIME_FORMAT"));
			return;
		}

		if (NewEventUseReminder && NewEventReminderMinutes <= 0)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("ERROR_REMINDER_MINUTES_POSITIVE"));
			return;
		}

		try
		{
			var scheduledLocal = NewEventDate.Date.AddHours(hours).AddMinutes(minutes);
			var scheduledUtc = DateTime.SpecifyKind(scheduledLocal, DateTimeKind.Utc);
			TimeSpan? reminder = NewEventUseReminder ? TimeSpan.FromMinutes(NewEventReminderMinutes) : null;

			var request = new CreateCalendarEventRequest(
				_userId.Value,
				NewEventTitle.Trim(),
				string.IsNullOrWhiteSpace(NewEventNotes) ? null : NewEventNotes.Trim(),
				scheduledUtc,
				reminder,
				NewEventRecurrence,
				null,
				NewEventAlertType);

			await _calendarService.CreateEventAsync(request, CancellationToken.None).ConfigureAwait(false);
			await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), Translate("INFO_ALERT_CREATED"));
			CancelNewEvent();
		}
		catch (Exception ex)
		{
			_interactionService.ShowInformation(Translate("NAVIGATION_CALENDAR"), ex.Message);
		}
	}

	private static bool TryParseTime(string text, out int hours, out int minutes)
	{
		hours = 0; minutes = 0;
		if (TimeSpan.TryParse(text, out var timeSpan))
		{
			hours = timeSpan.Hours;
			minutes = timeSpan.Minutes;
			return true;
		}

		return false;
	}

}

public enum CalendarRangeKind
{
	Day,
	Week,
	Month,
	Quarter,
	Year
}

public sealed record CalendarRangeOptionViewModel(CalendarRangeKind Kind, string DisplayText);
