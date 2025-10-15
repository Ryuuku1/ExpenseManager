using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Domain.Entities.Calendar;
using ExpenseManager.Domain.Entities.Categories;
using ExpenseManager.Domain.Entities.Expenses;
using ExpenseManager.Domain.Entities.Users;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Persistence.Seed;

internal static class ExpenseManagerContextSeeder
{
    public static async Task SeedAsync(ExpenseManagerDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureCalendarEventRecurrenceColumnAsync(context, cancellationToken);

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            await SeedDefaultUserAsync(context, cancellationToken);
        }

        if (!await context.Categories.AnyAsync(cancellationToken))
        {
            await SeedDefaultCategoriesAsync(context, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        if (!await context.Expenses.AnyAsync(cancellationToken))
        {
            await SeedSampleExpensesAsync(context, cancellationToken);
        }

        if (!await context.CalendarEvents.AnyAsync(cancellationToken))
        {
            await SeedSampleCalendarEventsAsync(context, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDefaultUserAsync(ExpenseManagerDbContext context, CancellationToken cancellationToken)
    {
        var user = User.Create("Diogo Silva", "diogo@example.com", 2000m, "pt", Currency.Eur);
        await context.Users.AddAsync(user, cancellationToken);
    }

    private static async Task SeedDefaultCategoriesAsync(ExpenseManagerDbContext context, CancellationToken cancellationToken)
    {
        var categories = new List<Category>
        {
            Category.Create("Habitação", "Despesas com casa", null, true),
            Category.Create("Alimentação", "Supermercado e refeições", null, true),
            Category.Create("Transporte", "Combustível, transportes públicos", null, true),
            Category.Create("Lazer", "Entretenimento e hobbies", null, true),
            Category.Create("Saúde", "Consultas e farmácia", null, true),
            Category.Create("Educação", "Cursos e formação", null, true)
        };

        await context.Categories.AddRangeAsync(categories, cancellationToken);
    }

    private static async Task SeedSampleExpensesAsync(ExpenseManagerDbContext context, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .OrderBy(user => user.CreatedAt)
            .FirstAsync(cancellationToken);
        var categories = await context.Categories.AsNoTracking().ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var month = DateOnly.FromDateTime(now);

        var expenses = new List<Expense>
        {
            Expense.Create(user.Id, categories.First(c => c.Name == "Habitação").Id, "Renda Apartamento", Money.Create(750m, Currency.Eur), month.AddDays(-5), PaymentMethod.BankTransfer, ExpenseStatus.Approved),
            Expense.Create(user.Id, categories.First(c => c.Name == "Alimentação").Id, "Supermercado Semanal", Money.Create(120.45m, Currency.Eur), month.AddDays(-2), PaymentMethod.DebitCard, ExpenseStatus.Approved),
            Expense.Create(user.Id, categories.First(c => c.Name == "Transporte").Id, "Combustível", Money.Create(65.30m, Currency.Eur), month.AddDays(-1), PaymentMethod.CreditCard, ExpenseStatus.Approved),
            Expense.Create(user.Id, categories.First(c => c.Name == "Lazer").Id, "Cinema", Money.Create(28.90m, Currency.Eur), month.AddDays(-3), PaymentMethod.DigitalWallet, ExpenseStatus.Approved),
            Expense.Create(user.Id, categories.First(c => c.Name == "Saúde").Id, "Consulta Clínica", Money.Create(85m, Currency.Eur), month.AddDays(-7), PaymentMethod.CreditCard, ExpenseStatus.Approved)
        };

        await context.Expenses.AddRangeAsync(expenses, cancellationToken);
    }

    private static async Task SeedSampleCalendarEventsAsync(ExpenseManagerDbContext context, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .OrderBy(user => user.CreatedAt)
            .FirstAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var events = new List<CalendarEvent>
        {
            CalendarEvent.Create(user.Id, "Pagamento Ginásio", AlertType.PaymentReminder, now.AddDays(1), notes: "Pagamento mensal vence amanhã."),
            CalendarEvent.Create(user.Id, "Renda", AlertType.UpcomingBill, now.AddDays(3), notes: "Pagar renda mensal.", recurrence: RecurrenceType.Monthly),
            CalendarEvent.Create(user.Id, "Revisão orçamento", AlertType.BudgetLimit, now.AddDays(5), notes: "Verificar se o orçamento ainda está dentro do limite.")
        };

        await context.CalendarEvents.AddRangeAsync(events, cancellationToken);
    }

    private static async Task EnsureCalendarEventRecurrenceColumnAsync(ExpenseManagerDbContext context, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var hasColumn = false;

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info('CalendarEvents');";
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var columnName = reader.GetString(1);
                    if (string.Equals(columnName, "Recurrence", StringComparison.OrdinalIgnoreCase))
                    {
                        hasColumn = true;
                        break;
                    }
                }
            }

            if (!hasColumn)
            {
                await using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE \"CalendarEvents\" ADD COLUMN \"Recurrence\" INTEGER NOT NULL DEFAULT 0;";
                await alterCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}