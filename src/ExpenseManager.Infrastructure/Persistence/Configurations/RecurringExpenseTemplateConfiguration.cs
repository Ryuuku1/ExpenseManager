using ExpenseManager.Domain.Entities.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseManager.Infrastructure.Persistence.Configurations;

internal sealed class RecurringExpenseTemplateConfiguration : IEntityTypeConfiguration<RecurringExpenseTemplate>
{
    private static readonly ValueConverter<DateOnly, DateTime> DateOnlyUtcConverter = new(
        date => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        dateTime => DateOnly.FromDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)));

    private static readonly ValueConverter<DateOnly?, DateTime?> NullableDateOnlyUtcConverter = new(
        date => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null,
        dateTime => dateTime.HasValue
            ? DateOnly.FromDateTime(DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc))
            : null);

    private static readonly ValueComparer<DateOnly> DateOnlyValueComparer = new(
        (left, right) => left == right,
        value => value.GetHashCode(),
        value => value);

    private static readonly ValueComparer<DateOnly?> NullableDateOnlyValueComparer = new(
        (left, right) => left == right,
        value => value.HasValue ? value.Value.GetHashCode() : 0,
        value => value);

    public void Configure(EntityTypeBuilder<RecurringExpenseTemplate> builder)
    {
        builder.ToTable("RecurringExpenseTemplates");

        builder.HasKey(template => template.Id);

        builder.Property(template => template.UserId)
            .IsRequired();

        builder.Property(template => template.CategoryId)
            .IsRequired();

        builder.Property(template => template.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(template => template.Notes)
            .HasMaxLength(500);

        builder.Property(template => template.Recurrence)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(template => template.PaymentMethod)
            .HasConversion<int>()
            .IsRequired();

        var startDateProperty = builder.Property(template => template.StartDate)
            .HasConversion(DateOnlyUtcConverter)
            .IsRequired();

        startDateProperty.Metadata.SetValueComparer(DateOnlyValueComparer);

        var endDateProperty = builder.Property(template => template.EndDate)
            .HasConversion(NullableDateOnlyUtcConverter);

        endDateProperty.Metadata.SetValueComparer(NullableDateOnlyValueComparer);

        builder.OwnsOne(template => template.Amount, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(money => money.Currency)
                .HasColumnName("Currency")
                .HasConversion<int>()
                .IsRequired();
        });

        builder.Property(template => template.CreatedAt)
            .IsRequired();

        builder.Property(template => template.UpdatedAt)
            .IsRequired();
    }
}