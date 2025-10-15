using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Calendar.Services;
using ExpenseManager.Application.Categories.Services;
using ExpenseManager.Application.Dashboard.Services;
using ExpenseManager.Application.Expenses.Services;
using ExpenseManager.Application.RecurringExpenses.Services;
using ExpenseManager.Application.Reports.Services;
using ExpenseManager.Application.Users.Services;
using ExpenseManager.Application.Infrastructure;
using ExpenseManager.Infrastructure.Persistence;
using ExpenseManager.Infrastructure.Persistence.Seed;
using ExpenseManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExpenseManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ExpenseManagerDbContext>((serviceProvider, options) =>
        {
            var databasePath = ResolveDatabasePath(configuration);
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            options.UseSqlite($"Data Source={databasePath}");
            options.UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());
            options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IUserReadService, UserReadService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IRecurringExpenseTemplateService, RecurringExpenseTemplateService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();

        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExpenseManagerDbContext>();
        await ExpenseManagerContextSeeder.SeedAsync(context, cancellationToken);
    }

    private static string ResolveDatabasePath(IConfiguration configuration)
    {
        var configuredPath = configuration.GetConnectionString("ExpenseManager");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "ExpenseManager", "expense_manager.db");
    }
}