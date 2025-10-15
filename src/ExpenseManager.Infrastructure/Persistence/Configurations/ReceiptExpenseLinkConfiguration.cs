using ExpenseManager.Domain.Entities.Links;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseManager.Infrastructure.Persistence.Configurations;

internal sealed class ReceiptExpenseLinkConfiguration : IEntityTypeConfiguration<ReceiptExpenseLink>
{
    public void Configure(EntityTypeBuilder<ReceiptExpenseLink> builder)
    {
        builder.ToTable("ReceiptExpenseLinks");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.ExpenseId)
            .IsRequired();

        builder.Property(link => link.ReceiptId)
            .IsRequired();

        builder.Property(link => link.AllocationPercent)
            .HasPrecision(5, 2);

        builder.Property(link => link.Notes)
            .HasMaxLength(500);

        builder.OwnsOne(link => link.AllocationAmount, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("AllocationAmount")
                .HasPrecision(18, 2);

            moneyBuilder.Property(money => money.Currency)
                .HasColumnName("AllocationCurrency")
                .HasConversion<int>();
        });

        builder.OwnsOne(link => link.AuditTrail, auditBuilder =>
        {
            auditBuilder.Property(audit => audit.CreatedBy)
                .HasColumnName("CreatedBy")
                .IsRequired();

            auditBuilder.Property(audit => audit.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            auditBuilder.Property(audit => audit.UpdatedBy)
                .HasColumnName("UpdatedBy");

            auditBuilder.Property(audit => audit.UpdatedAt)
                .HasColumnName("UpdatedAt");
        });

        builder.HasIndex(link => new { link.ExpenseId, link.ReceiptId })
            .IsUnique();

        builder.HasIndex(link => link.ExpenseId);
        builder.HasIndex(link => link.ReceiptId);
    }
}
