using ExpenseManager.Domain.Entities.Calendar;
using ExpenseManager.Domain.Entities.Categories;
using ExpenseManager.Domain.Entities.Expenses;
using ExpenseManager.Domain.Entities.Links;
using ExpenseManager.Domain.Entities.Receipts;
using ExpenseManager.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Persistence;

public sealed class ExpenseManagerDbContext : DbContext
{
    public ExpenseManagerDbContext(DbContextOptions<ExpenseManagerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseReceipt> ExpenseReceipts => Set<ExpenseReceipt>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptExpenseLink> ReceiptExpenseLinks => Set<ReceiptExpenseLink>();
    public DbSet<RecurringExpenseTemplate> RecurringExpenseTemplates => Set<RecurringExpenseTemplate>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseManagerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}