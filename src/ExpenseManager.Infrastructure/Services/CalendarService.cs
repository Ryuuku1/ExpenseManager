using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Calendar.Models;
using ExpenseManager.Application.Calendar.Requests;
using ExpenseManager.Application.Calendar.Services;
using ExpenseManager.Application.Dashboard.Models;
using ExpenseManager.Domain.Entities.Calendar;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class CalendarService : ICalendarService
{
    private static readonly TimeSpan RecurringExpansionHorizon = TimeSpan.FromDays(365);

    private readonly ExpenseManagerDbContext _dbContext;

    public CalendarService(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CalendarEventItem>> GetUpcomingEventsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        if (toUtc < fromUtc)
        {
            return Array.Empty<CalendarEventItem>();
        }

        var singleEvents = await _dbContext.CalendarEvents
            .AsNoTracking()
            .Where(calendarEvent => calendarEvent.UserId == userId
                                     && calendarEvent.Recurrence == RecurrenceType.None
                                     && calendarEvent.ScheduledAt >= fromUtc
                                     && calendarEvent.ScheduledAt <= toUtc)
            .ToListAsync(cancellationToken);

        var recurringEvents = await _dbContext.CalendarEvents
            .AsNoTracking()
            .Where(calendarEvent => calendarEvent.UserId == userId
                                     && calendarEvent.Recurrence != RecurrenceType.None
                                     && calendarEvent.ScheduledAt <= toUtc)
            .ToListAsync(cancellationToken);

        var expanded = new List<CalendarEventItem>(singleEvents.Count + recurringEvents.Count);

        foreach (var calendarEvent in singleEvents)
        {
            if (IsDismissed(calendarEvent, calendarEvent.ScheduledAt))
            {
                continue;
            }

            expanded.Add(ToItem(calendarEvent, calendarEvent.ScheduledAt));
        }

        foreach (var calendarEvent in recurringEvents)
        {
            foreach (var occurrence in EnumerateOccurrences(calendarEvent, fromUtc, toUtc))
            {
                if (IsDismissed(calendarEvent, occurrence))
                {
                    continue;
                }

                expanded.Add(ToItem(calendarEvent, occurrence));
            }
        }

        return expanded
            .OrderBy(item => item.ScheduledAt)
            .ThenBy(item => item.Title)
            .ToList();
    }

    public async Task<IReadOnlyCollection<CalendarEventOccurrenceItem>> GetUpcomingOccurrencesAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        if (toUtc < fromUtc)
        {
            return Array.Empty<CalendarEventOccurrenceItem>();
        }

        var events = await _dbContext.CalendarEvents
            .AsNoTracking()
            .Where(calendarEvent => calendarEvent.UserId == userId && calendarEvent.ScheduledAt <= toUtc)
            .ToListAsync(cancellationToken);

        var occurrences = new List<CalendarEventOccurrenceItem>();

        foreach (var calendarEvent in events)
        {
            foreach (var occurrence in EnumerateOccurrences(calendarEvent, fromUtc, toUtc))
            {
                if (IsDismissed(calendarEvent, occurrence))
                {
                    continue;
                }

                occurrences.Add(new CalendarEventOccurrenceItem(
                    calendarEvent.Id,
                    calendarEvent.Title,
                    occurrence,
                    calendarEvent.EventType,
                    calendarEvent.Recurrence,
                    calendarEvent.Recurrence != RecurrenceType.None,
                    false));
            }
        }

        return occurrences
            .OrderBy(item => item.OccursAt)
            .ThenBy(item => item.Title)
            .ToList();
    }

    public async Task<IReadOnlyCollection<DashboardAlertItem>> GetDashboardRemindersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var horizonUtc = nowUtc.AddDays(30);
        var todayStartLocal = DateTime.Today;
        var todayEndLocal = todayStartLocal.AddDays(1);
        var todayStartUtc = todayStartLocal.ToUniversalTime();
        var todayEndUtc = todayEndLocal.ToUniversalTime();

        var occurrences = await GetUpcomingOccurrencesAsync(userId, nowUtc, horizonUtc, cancellationToken);

        return occurrences
            .Where(occurrence => occurrence.OccursAt >= todayStartUtc && occurrence.OccursAt < todayEndUtc)
            .Select(occurrence => new DashboardAlertItem(
                occurrence.EventId,
                occurrence.Title,
                occurrence.OccursAt,
                occurrence.EventType,
                occurrence.IsRecurring,
                occurrence.IsDismissed))
            .ToList();
    }

    public async Task<Guid> CreateEventAsync(CreateCalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        var scheduledAtUtc = NormalizeToUtc(request.ScheduledAt);
        var calendarEvent = CalendarEvent.Create(
            request.UserId,
            request.Title,
            request.EventType,
            scheduledAtUtc,
            request.Notes,
            request.ReminderOffset,
            request.LinkedExpenseId,
            request.Recurrence);

        await _dbContext.CalendarEvents.AddAsync(calendarEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return calendarEvent.Id;
    }

    public async Task<bool> UpdateEventAsync(UpdateCalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        var calendarEvent = await _dbContext.CalendarEvents
            .Where(item => item.Id == request.EventId && item.UserId == request.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (calendarEvent is null)
        {
            return false;
        }

        var scheduledAtUtc = NormalizeToUtc(request.ScheduledAt);
        calendarEvent.UpdateDetails(
            request.Title,
            request.EventType,
            scheduledAtUtc,
            request.Notes,
            request.ReminderOffset,
            request.Recurrence,
            request.LinkedExpenseId);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteEventAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var calendarEvent = await _dbContext.CalendarEvents
            .Where(item => item.Id == eventId && item.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken);

        if (calendarEvent is null)
        {
            return false;
        }

        _dbContext.CalendarEvents.Remove(calendarEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DismissOccurrenceAsync(DismissCalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        var calendarEvent = await _dbContext.CalendarEvents
            .Where(item => item.Id == request.EventId && item.UserId == request.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (calendarEvent is null)
        {
            return false;
        }

        var occurrenceUtc = NormalizeToUtc(request.OccurrenceUtc);
        calendarEvent.DismissUntil(occurrenceUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static DateTime NormalizeToUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }

    private static CalendarEventItem ToItem(CalendarEvent calendarEvent, DateTime scheduledAt)
    {
        return new CalendarEventItem(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Notes,
            scheduledAt,
            calendarEvent.ReminderOffset,
            calendarEvent.Recurrence,
            calendarEvent.ExpenseId,
            calendarEvent.EventType,
            calendarEvent.DismissedUntilUtc);
    }

    private static IEnumerable<DateTime> EnumerateOccurrences(DateTime startUtc, RecurrenceType recurrence, DateTime fromUtc, DateTime toUtc)
    {
        var occurrence = AlignToRange(startUtc, recurrence, fromUtc);

        while (occurrence <= toUtc)
        {
            if (occurrence >= fromUtc)
            {
                yield return occurrence;
            }

            var next = GetNextOccurrence(occurrence, recurrence);
            if (next <= occurrence)
            {
                yield break;
            }

            occurrence = next;
        }
    }

    private static IEnumerable<DateTime> EnumerateOccurrences(CalendarEvent calendarEvent, DateTime fromUtc, DateTime toUtc)
    {
        var effectiveToUtc = toUtc;

        if (calendarEvent.Recurrence != RecurrenceType.None)
        {
            var horizonUtc = calendarEvent.ScheduledAt + RecurringExpansionHorizon;
            if (effectiveToUtc > horizonUtc)
            {
                effectiveToUtc = horizonUtc;
            }
        }

        foreach (var occurrence in EnumerateOccurrences(calendarEvent.ScheduledAt, calendarEvent.Recurrence, fromUtc, effectiveToUtc))
        {
            yield return occurrence;
        }
    }

    private static bool IsDismissed(CalendarEvent calendarEvent, DateTime occurrenceUtc)
    {
        if (calendarEvent.DismissedUntilUtc is null)
        {
            return false;
        }

        return occurrenceUtc <= calendarEvent.DismissedUntilUtc.Value;
    }

    private static DateTime AlignToRange(DateTime startUtc, RecurrenceType recurrence, DateTime fromUtc)
    {
        if (startUtc >= fromUtc)
        {
            return startUtc;
        }

        return recurrence switch
        {
            RecurrenceType.Daily => startUtc.AddDays(Math.Ceiling((fromUtc - startUtc).TotalDays)),
            RecurrenceType.Weekly => startUtc.AddDays(Math.Ceiling((fromUtc - startUtc).TotalDays / 7d) * 7d),
            RecurrenceType.Monthly => AddMonthsUntil(startUtc, fromUtc, 1),
            RecurrenceType.Quarterly => AddMonthsUntil(startUtc, fromUtc, 3),
            RecurrenceType.Yearly => AddYearsUntil(startUtc, fromUtc, 1),
            _ => startUtc
        };
    }

    private static DateTime GetNextOccurrence(DateTime current, RecurrenceType recurrence)
    {
        return recurrence switch
        {
            RecurrenceType.Daily => current.AddDays(1),
            RecurrenceType.Weekly => current.AddDays(7),
            RecurrenceType.Monthly => current.AddMonths(1),
            RecurrenceType.Quarterly => current.AddMonths(3),
            RecurrenceType.Yearly => current.AddYears(1),
            _ => current
        };
    }

    private static DateTime AddMonthsUntil(DateTime start, DateTime target, int monthStep)
    {
        var monthsDifference = ((target.Year - start.Year) * 12) + target.Month - start.Month;
        if (target.Day > start.Day)
        {
            monthsDifference += 1;
        }

        var steps = Math.Max(0, (int)Math.Ceiling(monthsDifference / (double)monthStep));
        var candidate = start.AddMonths(steps * monthStep);

        while (candidate < target)
        {
            candidate = candidate.AddMonths(monthStep);
        }

        return candidate;
    }

    private static DateTime AddYearsUntil(DateTime start, DateTime target, int yearStep)
    {
        var yearsDifference = target.Year - start.Year;
        if (target.DayOfYear > start.DayOfYear)
        {
            yearsDifference += 1;
        }

        var steps = Math.Max(0, (int)Math.Ceiling(yearsDifference / (double)yearStep));
        var candidate = start.AddYears(steps * yearStep);

        while (candidate < target)
        {
            candidate = candidate.AddYears(yearStep);
        }

        return candidate;
    }
}
