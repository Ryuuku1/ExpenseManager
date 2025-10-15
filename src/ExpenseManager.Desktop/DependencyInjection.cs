using System;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.Services.Branding;
using ExpenseManager.Desktop.Services.Options;
using ExpenseManager.Desktop.ViewModels;
using ExpenseManager.Desktop.ViewModels.Dialogs;
using ExpenseManager.Desktop.Views.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseManager.Desktop;

public static class DependencyInjection
{
    public static IServiceCollection AddDesktop(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthenticationOptions>(configuration.GetSection(AuthenticationOptions.SectionName));
        services.Configure<BrandingOptions>(configuration.GetSection(BrandingOptions.SectionName));
        services.Configure<SupportOptions>(configuration.GetSection(SupportOptions.SectionName));
        services.AddOptions<LocalizationOptions>();
        services.AddSingleton<IAppDataDirectoryProvider, AppDataDirectoryProvider>();
        services.AddSingleton<ILocalizationManager, LocalizationManager>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IUserInteractionService, UserInteractionService>();
        services.AddSingleton<IAuthenticationStore, FileAuthenticationStore>();
        services.AddSingleton<IUserSessionService, UserSessionService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IBrandingService, BrandingService>();
        services.AddSingleton<ISupportService, SupportService>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ExpensesViewModel>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<CalendarViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<RecurringExpenseTemplateDialogViewModel>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddSingleton<Func<LoginWindow>>(sp => sp.GetRequiredService<LoginWindow>);

        services.AddSingleton<MainWindow>();

        return services;
    }
}