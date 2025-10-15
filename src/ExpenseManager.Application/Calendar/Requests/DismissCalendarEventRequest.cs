using System;

namespace ExpenseManager.Application.Calendar.Requests;

public sealed record DismissCalendarEventRequest(
    Guid UserId,
    Guid EventId,
    DateTime OccurrenceUtc);
