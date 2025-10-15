using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ExpenseManager.Application.Calendar.Models;
using ExpenseManager.Application.Calendar.Requests;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels.Dialogs;

public sealed partial class AlertEditorDialogViewModel : ObservableObject
{
    private Guid _alertId;
    private string? _existingNotes;
    private TimeSpan? _existingReminder;
    private RecurrenceType _existingRecurrence = RecurrenceType.None;
    private Guid? _existingExpenseId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DateTime _triggerDate = DateTime.Today;

    [ObservableProperty]
    private string _triggerTime = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    private AlertType _selectedAlertType = AlertType.Custom;

    public Array AlertTypes { get; } = Enum.GetValues(typeof(AlertType));

    public void Load(CalendarEventItem calendarEvent, DateTime occurrenceUtc)
    {
        _alertId = calendarEvent.Id;
        Title = calendarEvent.Title;

        var localDate = occurrenceUtc.Kind == DateTimeKind.Utc
            ? occurrenceUtc.ToLocalTime()
            : occurrenceUtc;

        TriggerDate = localDate.Date;
        TriggerTime = localDate.ToString("HH:mm");
        SelectedAlertType = calendarEvent.EventType;

        _existingNotes = calendarEvent.Notes;
        _existingReminder = calendarEvent.ReminderOffset;
        _existingRecurrence = calendarEvent.Recurrence;
    _existingExpenseId = calendarEvent.LinkedExpenseId;
    }

    public bool Validate(out string? message)
    {
        message = null;

        if (string.IsNullOrWhiteSpace(Title))
        {
            message = Translate("ERROR_ALERT_TITLE_REQUIRED");
            return false;
        }

        if (!TryParseTime(TriggerTime, out _, out _))
        {
            message = Translate("ERROR_ALERT_TIME_FORMAT");
            return false;
        }

        return true;
    }

    public UpdateCalendarEventRequest ToRequest(Guid userId)
    {
        var triggerDateTime = CombineDateAndTime(TriggerDate, TriggerTime);
        return new UpdateCalendarEventRequest(
            _alertId,
            userId,
            Title.Trim(),
            _existingNotes,
            triggerDateTime,
            _existingReminder,
            _existingRecurrence,
            _existingExpenseId,
            SelectedAlertType);
    }

    private static DateTime CombineDateAndTime(DateTime date, string timeText)
    {
        if (!TryParseTime(timeText, out var hours, out var minutes))
        {
            throw new InvalidOperationException(Translate("ERROR_ALERT_TIME_FORMAT"));
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
