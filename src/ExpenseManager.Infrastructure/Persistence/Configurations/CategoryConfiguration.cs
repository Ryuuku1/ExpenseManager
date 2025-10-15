using ExpenseManager.Domain.Entities.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseManager.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasMaxLength(400);

        builder.Property(category => category.IsDefault)
            .HasDefaultValue(false);

        builder.HasOne<Category>()
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => new { category.Name, category.ParentId })
            .IsUnique();

        builder.Navigation(category => category.Children)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}