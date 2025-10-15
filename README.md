dotnet restore
dotnet build -c Release
dotnet run --project src/ExpenseManager.Desktop
dotnet publish src/ExpenseManager.Desktop -c Release -p:GenerateAppxPackageOnBuild=true
# ExpenseManager

ExpenseManager is a modern Windows desktop application for managing personal finances. It targets .NET 9 (WPF) and adopts a clean MVVM architecture with layered projects for domain, application, infrastructure, and presentation concerns. The app is localization aware, brandable, Microsoft Store ready, and designed to help individuals stay on top of day‑to‑day spending.

## Table of Contents

- [Highlights](#highlights)
- [Screens and Workflows](#screens-and-workflows)
- [Architecture Overview](#architecture-overview)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Localization Guide](#localization-guide)
- [Branding and Theming](#branding-and-theming)
- [Database and Data Management](#database-and-data-management)
- [Packaging and Store Submission](#packaging-and-store-submission)
- [Testing](#testing)
- [Logging and Diagnostics](#logging-and-diagnostics)
- [Troubleshooting](#troubleshooting)
- [Roadmap Ideas](#roadmap-ideas)
- [Contributing](#contributing)
- [Support the Project](#support-the-project)
- [License](#license)

## Highlights

- **Real-time dashboard**: Track monthly totals, pending alerts, recent expenses, and quick actions in a single view.
- **Expense lifecycle**: Create, update, delete, or duplicate expenses with attachments, payment tracking, due dates, reminders, and category metadata.
- **Recurring templates**: Define reusable templates for frequent expenses to save time and preserve consistency.
- **Calendar and alerts**: Manage financial events, recurring reminders, acknowledgments, and dismissals from a rich calendar surface.
- **Insightful reports**: Visualize spending by category, time period, and status with several chart types and summary cards.
- **Flexible categories**: Maintain custom categories with localized names, descriptions, and default badges.
- **Localization built-in**: Switch languages on the fly (Portuguese default, English included) without restarting the app.
- **Branding and themes**: Apply different color palettes, custom logos, and window icons to match personal style or company branding.
- **Data portability**: Export backups of the SQLite database and restore them later.
- **Microsoft Store ready**: MSIX packaging and manifest metadata are preconfigured for distribution through the Microsoft Store.

## Screens and Workflows

- **Dashboard**: Overview of the current month, alerts, spending trend, and quick navigation to commonly used features.
- **Expenses**: Detailed list with search, filters, payment status, inline editing, and attachment management.
- **Categories**: CRUD for categories plus quick identification of default ones and usage counts.
- **Calendar**: Day, week, month, quarter, and year views; event creation with optional alerts and reminders.
- **Reports**: Summary statistics, charts, and tables highlighting trends and totals.
- **Settings**: Profile update (name, email, preferred currency), language selection, backup creation, theme selection, branding assets, support links, and localization toggles.
- **Authentication**: Login dialog with localization-aware validation and optional user creation.

## Architecture Overview

ExpenseManager follows a layered architecture with strong separation of concerns:

| Project | Responsibility |
| --- | --- |
| `ExpenseManager.Domain` | Core domain model, entities, value objects, and enumerations. |
| `ExpenseManager.Application` | Application services, DTOs, and use-case orchestration. |
| `ExpenseManager.Infrastructure` | Persistence (EF Core + SQLite), data seeding, concrete service implementations. |
| `ExpenseManager.Desktop` | WPF presentation layer built with MVVM, localization, theming, and packaging assets. |
| `tests/ExpenseManager.Tests` | xUnit test project (currently placeholders for future automation). |

Key architectural traits:

- **MVVM with CommunityToolkit.Mvvm** for state management, commands, and observable properties.
- **Dependency Injection** via `Microsoft.Extensions.DependencyInjection`, configured centrally in each project.
- **Localization pipeline** using `LocalizationManager`, `LocalizationService`, and `TranslationSource` to load JSON resources, control cultures, and broadcast updates.
- **SQLite persistence** with Entity Framework Core. Databases live under `%LOCALAPPDATA%/ExpenseManager`. Seeding ensures default data (including an initial user) on first run.
- **Serilog logging** to rolling files and the console.
- **MSIX packaging** integrated in the desktop project for store-ready builds.

## Technology Stack

- .NET 9.0 (C# 13)
- WPF (Windows Presentation Foundation)
- Entity Framework Core + SQLite
- CommunityToolkit.Mvvm
- Microsoft.Extensions.* (Configuration, Hosting, Logging, Dependency Injection)
- Serilog (console + file sinks)
- MSIX packaging tooling

## Getting Started

### 1. Prerequisites

- Windows 10 or 11 (x64)
- .NET 9.0 SDK
- Visual Studio 2022 (17.10+) with desktop development workload, or VS Code with C# Dev Kit
- PowerShell 5.1+ (included with Windows)

### 2. Clone the Repository

```powershell
git clone https://github.com/Ryuuku1/ExpenseManager.git
cd ExpenseManager
```

### 3. Restore Dependencies and Build

```powershell
dotnet restore
dotnet build -c Release
```

If the build fails because `ExpenseManager.Desktop.exe` is locked, close any running instance of the app and retry.

### 4. Run the Desktop App

```powershell
dotnet run --project src/ExpenseManager.Desktop
```

On first launch the database is created automatically and seeded. Default credentials (from `appsettings.json`) are:

```
Username: gestor
Password: 123456
```

Use Settings > Support to trigger the PayPal donation link, or Settings > Backup to export the SQLite database.

## Configuration

Primary configuration lives in `src/ExpenseManager.Desktop/appsettings.json`.

- `Authentication`: bootstrap credentials used when seeding the first user.
- `Branding`: default color scheme and optional logo/icon overrides.
- `Support`: PayPal email and currency used by the donation flow.
- `Serilog`: logging level and sink configuration.

Additional configuration highlights:

- Connection string resolution happens in `ExpenseManager.Infrastructure.DependencyInjection`. By default the SQLite database is located in `%LOCALAPPDATA%/ExpenseManager/expense_manager.db`.
- `Package.appxmanifest` contains MSIX identity, capabilities, and visual assets for store packaging.
- Localization JSON resources live under `src/ExpenseManager.Desktop/Resources/Localization`.

## Localization Guide

ExpenseManager localizes UI strings through culture-specific JSON files (flat key/value pairs). Key components include:

- `LocalizationManager`: Discovers supported cultures, loads resources (including flat files such as `en-US.json`), caches values, and raises `CultureChanged` events.
- `LocalizationService`: Applies cultures to the WPF dispatcher thread, updates `FrameworkElement.Language`, and triggers a global translation refresh.
- `TranslationSource`: Works with XAML bindings to provide localized strings in markup.
- Session and settings integration: When users change language preferences, the session is updated immediately to keep navigation consistent.

### Adding a New Language

1. Create `src/ExpenseManager.Desktop/Resources/Localization/<culture>.json` (for example `es-ES.json`).
2. Copy keys from `en-US.json` or `pt-PT.json` and translate the values.
3. (Optional) Add nested JSON objects; the flattening algorithm supports dot notation keys.
4. Launch the app and select the new language in Settings > Preferred Language.

`LocalizationManager` logs any missing keys per culture at startup. Check `%LOCALAPPDATA%/ExpenseManager/logs/expense_manager.log` after adding translations.

## Branding and Theming

- Theming is driven by `BrandingColorScheme` (Midnight, Emerald, Sunset, Aurora, Ocean, Pinky, Salmon, Blush, BlushLight).
- Settings allow end users to apply color schemes, upload custom logos, and set window icons. These assets persist in user preferences.
- Developers can extend color palettes via resource dictionaries in `src/ExpenseManager.Desktop/Resources/Palettes`.
- Branding updates are propagated through `BrandingService`, which notifies view models via events.

## Database and Data Management

- **Provider**: SQLite (configured via `UseSqlite`).
- **Context**: `ExpenseManagerDbContext` with DbSet collections for users, categories, expenses, receipts, recurring templates, and calendar events.
- **Migrations/Seeding**: `ExpenseManagerContextSeeder` seeds initial data (default user, categories, sample expenses) if the database is empty.
- **Backup**: Settings view exposes a database backup command via `IDatabaseBackupService`, saving a `.db` file to a user-selected location.
- **Location**: `%LOCALAPPDATA%/ExpenseManager/expense_manager.db` by default; override through the `ExpenseManager` connection string.

## Packaging and Store Submission

### Create an MSIX Package

```powershell
dotnet publish src/ExpenseManager.Desktop/ExpenseManager.Desktop.csproj \
	-c Release \
	-f net9.0-windows10.0.19041.0 \
	-p:GenerateAppxPackageOnBuild=true \
	-p:PublishSingleFile=false \
	-p:RuntimeIdentifier=win10-x64
```

The output lives under `src/ExpenseManager.Desktop/bin/Release/net9.0-windows10.0.19041.0/win10-x64/AppPackages` and includes an MSIX package and related artifacts.

### Signing and Certificates

- Update `Package.appxmanifest` with your Publisher display name and identity before packaging.
- Configure a signing certificate (*.pfx) either through MSBuild properties or the MSIX Packaging Tool.

### Publish to Microsoft Store

1. Sign in to [Partner Center](https://partner.microsoft.com/).
2. Reserve a new app name or open an existing listing.
3. Upload the generated MSIX package, provide Store metadata (description, screenshots, pricing, category), and specify target devices.
4. Submit for certification. Microsoft usually processes updates within 24–72 hours.

## Testing

Automated tests live in `tests/ExpenseManager.Tests`. At present the project is a scaffold; populate it with unit and integration tests as you evolve the codebase.

Run the entire test suite:

```powershell
dotnet test
```

## Logging and Diagnostics

- Serilog writes structured logs to `%LOCALAPPDATA%/ExpenseManager/logs/expense_manager.log`.
- Startup errors produce a `.startup-error.log` file alongside the main log.
- Adjust logging levels through `appsettings.json` or environment variables.

## Troubleshooting

- **Build fails with MSB3021/MSB3027**: Close running instances of the app so MSBuild can overwrite `ExpenseManager.Desktop.exe`.
- **Missing localization strings**: Check Serilog warnings for cultures missing keys. Add the missing keys to the relevant JSON resource file.
- **Database locked**: Ensure the app is not running when replacing/overwriting the SQLite database file. Use the backup feature to create clean copies.
- **Partner Center rejection**: Verify `Package.appxmanifest` capabilities, icon sizes, and the signing certificate validity.

## Roadmap Ideas

- Expand the test suite with UI automation, localization coverage tests, and service unit tests.
- Add cloud synchronization or multi-device support.
- Introduce budgeting goals and variance analytics.
- Provide richer export formats (CSV/PDF for reports, ICS for calendar).
- Offer plugins for currency exchange, bank statement imports, or OCR receipt processing.

## Contributing

Contributions are welcome. Suggested flow:

1. Fork the repository and create a feature branch.
2. Implement your changes with clear commit messages and minimal diff noise.
3. Update documentation and localization files if you introduce new UI strings.
4. Run `dotnet build` and `dotnet test` to validate.
5. Open a pull request describing the motivation, testing, and screenshots if applicable.

## Support the Project

If you find ExpenseManager useful, you can "buy a coffee" through the built-in PayPal integration or by donating directly to `silv4.diogo@gmail.com` on PayPal.

## License

No explicit license has been supplied yet. Add your preferred license (MIT, Apache-2.0, etc.) before distributing binaries.
