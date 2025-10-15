using ExpenseManager.Domain.Entities.Calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseManager.Infrastructure.Persistence.Configurations;

internal sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("CalendarEvents");

        builder.HasKey(calendarEvent => calendarEvent.Id);

        builder.Property(calendarEvent => calendarEvent.UserId)
            .IsRequired();

        builder.Property(calendarEvent => calendarEvent.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(calendarEvent => calendarEvent.Notes)
            .HasMaxLength(500);

        builder.Property(calendarEvent => calendarEvent.ScheduledAt)
            .IsRequired();

        builder.Property(calendarEvent => calendarEvent.ReminderOffset)
            .HasConversion(
                timeSpan => timeSpan.HasValue ? timeSpan.Value.Ticks : (long?)null,
                ticks => ticks.HasValue ? TimeSpan.FromTicks(ticks.Value) : null);

        builder.Property(calendarEvent => calendarEvent.Recurrence)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(calendarEvent => calendarEvent.CreatedAt)
            .IsRequired();

        builder.Property(calendarEvent => calendarEvent.UpdatedAt)
            .IsRequired();
    }
}