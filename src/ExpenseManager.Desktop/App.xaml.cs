using System;
using ExpenseManager.Application;
using ExpenseManager.Application.Users.Services;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Infrastructure;
using ExpenseManager.Desktop.Services.Branding;
using ExpenseManager.Desktop.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace ExpenseManager.Desktop;

public partial class App : global::System.Windows.Application
{
	private IHost? _host;

	public static IServiceProvider? Services => (Current as App)?._host?.Services;

	protected override async void OnStartup(System.Windows.StartupEventArgs e)
	{
		base.OnStartup(e);

		try
		{
			RegisterGlobalExceptionHandlers();
			_host = CreateHostBuilder().Build();
			await _host.Services.EnsureDatabaseCreatedAsync();
			await ApplyInitialCultureAsync(_host.Services);
			TranslationSource.Instance.Initialize(_host.Services);
			var brandingService = _host.Services.GetRequiredService<IBrandingService>();
			await brandingService.InitializeAsync();

			var mainWindow = _host.Services.GetRequiredService<MainWindow>();
			mainWindow.Show();
		}
		catch (Exception exception)
		{
			Log.Fatal(exception, "Unhandled exception during application startup");
			var diagnosticsPath = System.IO.Path.ChangeExtension(GetLogPath(), ".startup-error.log");
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(diagnosticsPath)!);
			await System.IO.File.WriteAllTextAsync(diagnosticsPath, exception.ToString());
			Console.Error.WriteLine(exception);
			await Log.CloseAndFlushAsync();
			Environment.Exit(1);
		}
	}

	private static async Task ApplyInitialCultureAsync(IServiceProvider services)
	{
		await using var scope = services.CreateAsyncScope();
		var provider = scope.ServiceProvider;
		var localizationService = provider.GetRequiredService<ILocalizationService>();
	var authenticationStore = provider.GetRequiredService<IAuthenticationStore>();
		var userReadService = provider.GetRequiredService<IUserReadService>();
		var userProfileService = provider.GetRequiredService<IUserProfileService>();

		var accounts = await authenticationStore.GetAllAsync(CancellationToken.None);
		var preferredLanguage = accounts
			.OrderByDescending(account => account.LastAuthenticatedAtUtc ?? DateTime.MinValue)
			.Select(account => account.PreferredLanguage)
			.FirstOrDefault(language => !string.IsNullOrWhiteSpace(language));

		if (!string.IsNullOrWhiteSpace(preferredLanguage))
		{
			if (localizationService.TryApplyCulture(preferredLanguage, out _))
			{
				return;
			}
		}

		var userId = await userReadService.GetDefaultUserIdAsync(CancellationToken.None);
		if (userId is null)
		{
			return;
		}

		var profile = await userProfileService.GetProfileAsync(userId.Value, CancellationToken.None);
		if (profile is null || string.IsNullOrWhiteSpace(profile.PreferredLanguage))
		{
			return;
		}

		localizationService.TryApplyCulture(profile.PreferredLanguage, out _);
	}


	private static void RegisterGlobalExceptionHandlers()
	{
		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			if (args.ExceptionObject is not Exception ex)
			{
				return;
			}

			Log.Fatal(ex, "Unhandled domain exception");
			Console.Error.WriteLine(ex);
		};

		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			Log.Fatal(args.Exception, "Unobserved task exception");
			Console.Error.WriteLine(args.Exception);
			args.SetObserved();
		};

		Current.DispatcherUnhandledException += (_, args) =>
		{
			Log.Fatal(args.Exception, "Unhandled dispatcher exception");
			Console.Error.WriteLine(args.Exception);
			args.Handled = true;
		};
	}

	protected override async void OnExit(System.Windows.ExitEventArgs e)
	{
		if (_host is not null)
		{
			await _host.StopAsync();
			_host.Dispose();
		}

		await Log.CloseAndFlushAsync();
		base.OnExit(e);
	}

	private static IHostBuilder CreateHostBuilder()
	{
		return Host.CreateDefaultBuilder()
			.UseSerilog((context, services, configuration) =>
			{
				configuration
					.ReadFrom.Configuration(context.Configuration)
					.ReadFrom.Services(services)
					.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
					.WriteTo.Console()
					.WriteTo.File(
						path: GetLogPath(),
						rollingInterval: RollingInterval.Day,
						restrictedToMinimumLevel: LogEventLevel.Information,
						retainedFileCountLimit: 7);
			})
			.ConfigureAppConfiguration((_, configurationBuilder) =>
			{
				configurationBuilder
					.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
					.AddEnvironmentVariables();
			})
			.ConfigureServices((context, services) =>
			{
				services.AddLogging();
				services.AddApplication();
				services.AddInfrastructure(context.Configuration);
				services.AddDesktop(context.Configuration);
			});
	}

	private static string GetLogPath()
	{
		var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		return Path.Combine(appData, "ExpenseManager", "logs", "expense_manager.log");
	}
}