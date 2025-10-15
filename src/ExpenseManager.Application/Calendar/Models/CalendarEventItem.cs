using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Calendar.Models;

public sealed record CalendarEventItem(
    Guid Id,
    string Title,
    string? Notes,
    DateTime ScheduledAt,
    TimeSpan? ReminderOffset,
    RecurrenceType Recurrence,
    Guid? LinkedExpenseId,
    AlertType EventType,
    DateTime? DismissedUntilUtc);
