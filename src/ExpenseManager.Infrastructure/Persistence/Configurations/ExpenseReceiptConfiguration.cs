using ExpenseManager.Domain.Entities.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseManager.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseReceiptConfiguration : IEntityTypeConfiguration<ExpenseReceipt>
{
    public void Configure(EntityTypeBuilder<ExpenseReceipt> builder)
    {
        builder.ToTable("ExpenseReceipts");

        builder.HasKey(receipt => receipt.Id);

        builder.Property(receipt => receipt.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(receipt => receipt.FilePath)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(receipt => receipt.FileSizeInBytes)
            .IsRequired();

        builder.Property(receipt => receipt.UploadedAt)
            .IsRequired();

        builder.Property(receipt => receipt.ExpenseId)
            .IsRequired();
    }
}