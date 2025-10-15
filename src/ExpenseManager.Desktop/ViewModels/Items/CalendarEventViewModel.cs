using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class CalendarEventViewModel
{
    public CalendarEventViewModel(Guid id, string title, string? notes, DateTime scheduledAt, TimeSpan? reminderOffset, AlertType eventType, RecurrenceType recurrence, Guid? linkedExpenseId, DateTime? dismissedUntilUtc, string recurrenceDisplay, string reminderDisplay, string eventTypeDisplay)
    {
        Id = id;
        Title = title;
        Notes = notes;
        ScheduledAt = scheduledAt;
        ReminderOffset = reminderOffset;
        EventType = eventType;
        Recurrence = recurrence;
        LinkedExpenseId = linkedExpenseId;
        DismissedUntilUtc = dismissedUntilUtc;
        RecurrenceDisplay = recurrenceDisplay;
        ReminderDisplay = reminderDisplay;
        EventTypeDisplay = eventTypeDisplay;
    }

    public Guid Id { get; }
    public string Title { get; }
    public string? Notes { get; }
    public DateTime ScheduledAt { get; }
    public TimeSpan? ReminderOffset { get; }
    public AlertType EventType { get; }
    public RecurrenceType Recurrence { get; }
    public Guid? LinkedExpenseId { get; }
    public DateTime? DismissedUntilUtc { get; }
    public string RecurrenceDisplay { get; }
    public string ReminderDisplay { get; }
    public string EventTypeDisplay { get; }
    public bool IsDismissed => DismissedUntilUtc is not null && ScheduledAt <= DismissedUntilUtc.Value;
}
