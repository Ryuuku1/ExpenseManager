using ExpenseManager.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseManager.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.PreferredLanguage)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(user => user.MonthlyBudget)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(user => user.PreferredCurrency)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();
    }
}