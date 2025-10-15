using System;
using ExpenseManager.Domain.Abstractions;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Domain.Entities.Calendar;

public sealed class CalendarEvent : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid? ExpenseId { get; private set; }
    public string Title { get; private set; }
    public string? Notes { get; private set; }
    public AlertType EventType { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public TimeSpan? ReminderOffset { get; private set; }
    public RecurrenceType Recurrence { get; private set; }
    public DateTime? DismissedUntilUtc { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CalendarEvent()
    {
        Title = string.Empty;
        EventType = AlertType.Custom;
    }

    private CalendarEvent(Guid id, Guid userId, string title, string? notes, AlertType eventType, DateTime scheduledAt, TimeSpan? reminderOffset, Guid? expenseId, RecurrenceType recurrence) : base(id)
    {
        UserId = userId;
        Title = title;
        Notes = notes;
        EventType = eventType;
        ScheduledAt = scheduledAt;
        ReminderOffset = reminderOffset;
        ExpenseId = expenseId;
        Recurrence = recurrence;
        DismissedUntilUtc = null;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static CalendarEvent Create(Guid userId, string title, AlertType eventType, DateTime scheduledAt, string? notes = null, TimeSpan? reminderOffset = null, Guid? expenseId = null, RecurrenceType recurrence = RecurrenceType.None)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        return new CalendarEvent(Guid.NewGuid(), userId, title.Trim(), notes?.Trim(), eventType, scheduledAt, reminderOffset, expenseId, recurrence);
    }

    public void UpdateDetails(string title, AlertType eventType, DateTime scheduledAt, string? notes, TimeSpan? reminderOffset, RecurrenceType recurrence, Guid? expenseId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        Title = title.Trim();
        Notes = notes?.Trim();
        EventType = eventType;
        ScheduledAt = scheduledAt;
        ReminderOffset = reminderOffset;
        Recurrence = recurrence;
        ExpenseId = expenseId;
        ResetDismissalIfNecessary(scheduledAt);
        Touch();
    }

    public void DismissUntil(DateTime occurrenceUtc)
    {
        DismissedUntilUtc = occurrenceUtc;
        Touch();
    }

    private void ResetDismissalIfNecessary(DateTime scheduledAt)
    {
        if (DismissedUntilUtc is null)
        {
            return;
        }

        if (Recurrence == RecurrenceType.None && DismissedUntilUtc.Value <= scheduledAt)
        {
            DismissedUntilUtc = null;
            return;
        }

        if (Recurrence != RecurrenceType.None && DismissedUntilUtc.Value < scheduledAt)
        {
            DismissedUntilUtc = null;
        }
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}