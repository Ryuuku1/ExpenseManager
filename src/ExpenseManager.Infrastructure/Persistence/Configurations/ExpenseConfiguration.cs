using ExpenseManager.Domain.Entities.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseManager.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
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

    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.HasKey(expense => expense.Id);

        builder.Property(expense => expense.UserId)
            .IsRequired();

        builder.Property(expense => expense.CategoryId)
            .IsRequired();

        builder.Property(expense => expense.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(expense => expense.Description)
            .HasMaxLength(500);

        builder.Property(expense => expense.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(expense => expense.PaymentMethod)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(expense => expense.Recurrence)
            .HasConversion<int>()
            .IsRequired();

        var expenseDateProperty = builder.Property(expense => expense.ExpenseDate)
            .HasConversion(DateOnlyUtcConverter)
            .IsRequired();

        expenseDateProperty.Metadata.SetValueComparer(DateOnlyValueComparer);

        var dueDateProperty = builder.Property(expense => expense.DueDate)
            .HasConversion(NullableDateOnlyUtcConverter);

        dueDateProperty.Metadata.SetValueComparer(NullableDateOnlyValueComparer);

        builder.Property(expense => expense.CreatedAt)
            .IsRequired();

        builder.Property(expense => expense.UpdatedAt)
            .IsRequired();

        builder.OwnsOne(expense => expense.Amount, moneyBuilder =>
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

        builder.HasMany(expense => expense.Receipts)
            .WithOne()
            .HasForeignKey(receipt => receipt.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(expense => expense.Receipts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(expense => new { expense.UserId, expense.ExpenseDate });
    }
}