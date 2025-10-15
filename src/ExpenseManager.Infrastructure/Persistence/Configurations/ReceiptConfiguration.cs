using ExpenseManager.Domain.Entities.Receipts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseManager.Infrastructure.Persistence.Configurations;

internal sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    private static readonly ValueConverter<DateOnly, DateTime> DateOnlyUtcConverter = new(
        date => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        dateTime => DateOnly.FromDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)));

    private static readonly ValueComparer<DateOnly> DateOnlyValueComparer = new(
        (left, right) => left == right,
        value => value.GetHashCode(),
        value => value);

    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(receipt => receipt.Id);

        builder.Property(receipt => receipt.UserId)
            .IsRequired();

        builder.Property(receipt => receipt.CategoryId);

        builder.Property(receipt => receipt.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(receipt => receipt.Description)
            .HasMaxLength(500);

        builder.Property(receipt => receipt.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(receipt => receipt.Vendor)
            .HasMaxLength(200);

        var receiptDateProperty = builder.Property(receipt => receipt.ReceiptDate)
            .HasConversion(DateOnlyUtcConverter)
            .IsRequired();

        receiptDateProperty.Metadata.SetValueComparer(DateOnlyValueComparer);

        builder.Property(receipt => receipt.FileName)
            .HasMaxLength(260);

        builder.Property(receipt => receipt.FilePath)
            .HasMaxLength(1024);

        builder.Property(receipt => receipt.FileSizeInBytes);

        builder.Property(receipt => receipt.CommissionPercent)
            .HasPrecision(5, 2);

        builder.Property(receipt => receipt.CapturedAt)
            .IsRequired();

        builder.Property(receipt => receipt.UpdatedAt)
            .IsRequired();

        builder.OwnsOne(receipt => receipt.NetAmount, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("NetAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(money => money.Currency)
                .HasColumnName("NetCurrency")
                .HasConversion<int>()
                .IsRequired();
        });

        builder.HasMany(receipt => receipt.ExpenseLinks)
            .WithOne()
            .HasForeignKey(link => link.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(receipt => receipt.ExpenseLinks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(receipt => new { receipt.UserId, receipt.ReceiptDate });
        builder.HasIndex(receipt => new { receipt.UserId, receipt.CategoryId });
    }
}
