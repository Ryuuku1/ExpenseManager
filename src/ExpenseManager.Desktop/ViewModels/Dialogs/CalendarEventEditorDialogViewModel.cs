using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ExpenseManager.Application.Calendar.Requests;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels.Dialogs;

public sealed partial class CalendarEventEditorDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private DateTime _eventDate = DateTime.Today;

    [ObservableProperty]
    private string _eventTime = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    private bool _useReminder;

    [ObservableProperty]
    private int _reminderMinutes = 30;

    [ObservableProperty]
    private bool _createAlert;

    [ObservableProperty]
    private string _alertTitle = string.Empty;

    [ObservableProperty]
    private DateTime _alertDate = DateTime.Today;

    [ObservableProperty]
    private string _alertTime = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    private AlertType _selectedAlertType = AlertType.Custom;

    public Array AlertTypes { get; } = Enum.GetValues(typeof(AlertType));
    public Array RecurrenceTypes { get; } = Enum.GetValues(typeof(RecurrenceType));

    [ObservableProperty]
    private RecurrenceType _selectedRecurrence = RecurrenceType.None;

    public bool Validate(out string? message)
    {
        message = null;

        if (string.IsNullOrWhiteSpace(Title))
        {
                message = Translate("ERROR_EVENT_TITLE_REQUIRED");
            return false;
        }

        if (!TryParseTime(EventTime, out _, out _))
        {
                message = Translate("ERROR_EVENT_TIME_FORMAT");
            return false;
        }

        if (UseReminder && ReminderMinutes <= 0)
        {
                message = Translate("ERROR_REMINDER_MINUTES_POSITIVE");
            return false;
        }

        if (CreateAlert)
        {
            if (string.IsNullOrWhiteSpace(AlertTitle))
            {
                    message = Translate("ERROR_ALERT_TITLE_REQUIRED");
                return false;
            }

            if (!TryParseTime(AlertTime, out _, out _))
            {
                    message = Translate("ERROR_ALERT_TIME_FORMAT");
                return false;
            }
        }

        return true;
    }

    public CreateCalendarEventRequest ToRequest(Guid userId)
    {
        var scheduledAt = CombineDateAndTime(EventDate, EventTime);
        TimeSpan? reminder = UseReminder ? TimeSpan.FromMinutes(ReminderMinutes) : null;

        var effectiveTitle = CreateAlert && !string.IsNullOrWhiteSpace(AlertTitle)
            ? AlertTitle.Trim()
            : Title.Trim();

        var eventType = CreateAlert
            ? SelectedAlertType
            : AlertType.Custom;

        return new CreateCalendarEventRequest(
            userId,
            effectiveTitle,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            scheduledAt,
            reminder,
            SelectedRecurrence,
            null,
            eventType);
    }

    private static DateTime CombineDateAndTime(DateTime date, string timeText)
    {
        if (!TryParseTime(timeText, out var hours, out var minutes))
        {
              throw new InvalidOperationException(Translate("ERROR_EVENT_TIME_FORMAT"));
        }

        var dateTime = date.Date.AddHours(hours).AddMinutes(minutes);
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    private static bool TryParseTime(string text, out int hours, out int minutes)
    {
        hours = 0;
        minutes = 0;

        if (TimeSpan.TryParse(text, out var timeSpan))
        {
            hours = timeSpan.Hours;
            minutes = timeSpan.Minutes;
            return true;
        }

        return false;
    }

    private static string Translate(string key) => TranslationSource.Instance[key];
}
