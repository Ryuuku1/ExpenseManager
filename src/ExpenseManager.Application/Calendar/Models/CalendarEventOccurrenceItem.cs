using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Calendar.Models;

public sealed record CalendarEventOccurrenceItem(
    Guid EventId,
    string Title,
    DateTime OccursAt,
    AlertType EventType,
    RecurrenceType Recurrence,
    bool IsRecurring,
    bool IsDismissed);
