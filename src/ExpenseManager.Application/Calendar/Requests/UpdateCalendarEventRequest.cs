using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Calendar.Requests;

public sealed record UpdateCalendarEventRequest(
    Guid EventId,
    Guid UserId,
    string Title,
    string? Notes,
    DateTime ScheduledAt,
    TimeSpan? ReminderOffset,
    RecurrenceType Recurrence,
    Guid? LinkedExpenseId,
    AlertType EventType);
