using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Calendar.Models;
using ExpenseManager.Application.Calendar.Requests;
using ExpenseManager.Application.Dashboard.Models;

namespace ExpenseManager.Application.Calendar.Services;

public interface ICalendarService
{
    Task<IReadOnlyCollection<CalendarEventItem>> GetUpcomingEventsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CalendarEventOccurrenceItem>> GetUpcomingOccurrencesAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DashboardAlertItem>> GetDashboardRemindersAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid> CreateEventAsync(CreateCalendarEventRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateEventAsync(UpdateCalendarEventRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteEventAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);

    Task<bool> DismissOccurrenceAsync(DismissCalendarEventRequest request, CancellationToken cancellationToken = default);
}
