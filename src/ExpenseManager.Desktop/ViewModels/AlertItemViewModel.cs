using System;
using System.Globalization;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels;

public sealed class AlertItemViewModel
{
    public AlertItemViewModel(Guid eventId, string title, DateTime occursAt, AlertType alertType, bool isRecurring, bool isDismissed, string alertTypeDisplay)
    {
        EventId = eventId;
        Title = title;
        OccursAt = occursAt;
        AlertType = alertType;
        IsRecurring = isRecurring;
        IsDismissed = isDismissed;
        AlertTypeDisplay = alertTypeDisplay;
    }

    public Guid EventId { get; }
    public string Title { get; }
    public DateTime OccursAt { get; }
    public DateTime OccursAtLocal => OccursAt.Kind == DateTimeKind.Utc ? OccursAt.ToLocalTime() : OccursAt;
    public string OccursAtDisplay => OccursAtLocal.ToString("g", CultureInfo.CurrentCulture);
    public AlertType AlertType { get; }
    public bool IsRecurring { get; }
    public bool IsDismissed { get; }
    public string AlertTypeDisplay { get; }
    public bool CanDeleteEvent => !IsRecurring;
}
