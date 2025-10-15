using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Application.Reports.Models;
using ExpenseManager.Application.Reports.Services;
using ExpenseManager.Application.Users.Services;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.ViewModels.Abstractions;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.ViewModels.Items;
using ExpenseManager.Domain.Enumerations;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WpfApplication = System.Windows.Application;

namespace ExpenseManager.Desktop.ViewModels;

public sealed partial class ReportsViewModel : ViewModelBase, ILoadableViewModel, ILocalizableViewModel
{
    private static readonly OxyColor DefaultChartBackgroundColor = OxyColor.FromRgb(0x1E, 0x2B, 0x3A);
    private static readonly OxyColor DefaultChartPlotAreaColor = OxyColor.FromRgb(0x18, 0x24, 0x33);
    private static readonly OxyColor DefaultChartGridlineColor = OxyColor.FromAColor(60, OxyColors.White);
    private static readonly OxyColor DefaultPrimaryTextColor = OxyColors.White;
    private static readonly OxyColor DefaultSecondaryTextColor = OxyColor.FromArgb(200, 0xE4, 0xD6, 0xFF);
    private static readonly OxyColor DefaultAccentColor = OxyColor.FromRgb(0xFF, 0x7D, 0xCB);

    private readonly IReportingService _reportingService;
    private readonly IUserSessionService _sessionService;
    private readonly IUserInteractionService _interactionService;
    private readonly ILocalizationManager _localization;
    private readonly TranslationSource _translationSource = TranslationSource.Instance;
    private MonthlyReportSummary? _lastSummary;
    private IReadOnlyCollection<SpendingByCategoryReportItem> _lastBreakdown = Array.Empty<SpendingByCategoryReportItem>();
    private IReadOnlyCollection<MonthlySpendingTrendPoint> _lastTrend = Array.Empty<MonthlySpendingTrendPoint>();

    private string _totalExpenses = string.Empty;
    public string TotalExpenses
    {
        get => _totalExpenses;
        private set => SetProperty(ref _totalExpenses, value);
    }

    private string _averageExpense = string.Empty;
    public string AverageExpense
    {
        get => _averageExpense;
        private set => SetProperty(ref _averageExpense, value);
    }

    private string _largestExpense = string.Empty;
    public string LargestExpense
    {
        get => _largestExpense;
        private set => SetProperty(ref _largestExpense, value);
    }

    private string _budget = string.Empty;
    public string Budget
    {
        get => _budget;
        private set => SetProperty(ref _budget, value);
    }

    private string _budgetRemaining = string.Empty;
    public string BudgetRemaining
    {
        get => _budgetRemaining;
        private set => SetProperty(ref _budgetRemaining, value);
    }

    private string _expenseCount = string.Empty;
    public string ExpenseCount
    {
        get => _expenseCount;
        private set => SetProperty(ref _expenseCount, value);
    }

    private PlotModel _categoryPlot = new();
    public PlotModel CategoryPlot
    {
        get => _categoryPlot;
        private set => SetProperty(ref _categoryPlot, value);
    }

    private PlotModel _trendPlot = new();
    public PlotModel TrendPlot
    {
        get => _trendPlot;
        private set => SetProperty(ref _trendPlot, value);
    }

    private ReportPeriodOptionViewModel? _selectedPeriod;

    public ReportPeriodOptionViewModel? SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                OnSelectedPeriodChanged(value);
            }
        }
    }

    private bool _hasCategoryData;
    public bool HasCategoryData
    {
        get => _hasCategoryData;
        private set => SetProperty(ref _hasCategoryData, value);
    }

    private bool _hasTrendData;
    public bool HasTrendData
    {
        get => _hasTrendData;
        private set => SetProperty(ref _hasTrendData, value);
    }

    public ObservableCollection<ReportCategoryBreakdownViewModel> CategoryBreakdown { get; } = new();

    public ObservableCollection<ReportPeriodOptionViewModel> PeriodOptions { get; } = new();

    private Guid? _userId;
    private bool _isInitializingPeriods;

    public ReportsViewModel(IReportingService reportingService, IUserSessionService sessionService, IUserInteractionService interactionService, ILocalizationManager localization)
    {
        _reportingService = reportingService;
        _sessionService = sessionService;
        _interactionService = interactionService;
        _localization = localization;

        _translationSource.PropertyChanged += OnTranslationSourceChanged;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _userId ??= _sessionService.UserId;
        if (_userId is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_REPORTS"), Translate("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        InitializePeriodOptions();

        if (SelectedPeriod is null)
        {
            return;
        }

        await ReloadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await ReloadAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        await ExportAsync(ReportExportFormat.Pdf, CancellationToken.None);
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        await ExportAsync(ReportExportFormat.Csv, CancellationToken.None);
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        if (_userId is null || SelectedPeriod is null)
        {
            return;
        }

        var selectedMonth = SelectedPeriod.Month;
        var (monthStart, monthEnd) = GetMonthBounds(selectedMonth);

        var summary = await _reportingService.GetMonthlySummaryAsync(_userId.Value, selectedMonth, cancellationToken);
        _lastSummary = summary;
        ApplySummary(summary);

        var breakdown = await _reportingService.GetSpendingByCategoryAsync(_userId.Value, monthStart, monthEnd, cancellationToken);
        _lastBreakdown = breakdown.ToList();
        ApplyCategoryBreakdown(_lastBreakdown);

        var trendStart = selectedMonth.AddMonths(-5);
        var trendData = await _reportingService.GetMonthlySpendingTrendAsync(_userId.Value, trendStart, selectedMonth, cancellationToken);
        _lastTrend = trendData.ToList();
        ApplyTrend(_lastTrend);
    }

    private void ApplySummary(MonthlyReportSummary summary)
    {
        TotalExpenses = FormatCurrency(summary.TotalExpenses, summary.Currency);
        AverageExpense = FormatCurrency(summary.AverageExpense, summary.Currency);
        LargestExpense = FormatCurrency(summary.LargestExpense, summary.Currency);
        Budget = FormatCurrency(summary.MonthlyBudget, summary.Currency);
        BudgetRemaining = FormatCurrency(summary.BudgetRemaining, summary.Currency);
        var culture = CultureInfo.CurrentUICulture;
        ExpenseCount = summary.ExpenseCount.ToString(culture);
    }

    private void ApplyCategoryBreakdown(IEnumerable<SpendingByCategoryReportItem> breakdown)
    {
        var localizedBreakdown = breakdown
            .Select(item => item with { CategoryName = CategoryLocalization.TranslateName(_localization, item.CategoryName) })
            .ToList();

        var palette = OxyPalettes.HueDistinct(Math.Max(3, localizedBreakdown.Count));
        var breakdownItems = new List<ReportCategoryBreakdownViewModel>(localizedBreakdown.Count);

        for (var index = 0; index < localizedBreakdown.Count; index++)
        {
            var item = localizedBreakdown[index];
            var paletteColor = palette.Colors[index % palette.Colors.Count];
            var mediaColor = System.Windows.Media.Color.FromArgb(paletteColor.A, paletteColor.R, paletteColor.G, paletteColor.B);
            var brush = new System.Windows.Media.SolidColorBrush(mediaColor);
            if (!brush.IsFrozen)
            {
                brush.Freeze();
            }

            breakdownItems.Add(new ReportCategoryBreakdownViewModel(item.CategoryName, item.TotalAmount, item.Currency, item.ExpenseCount, brush));
        }

        CategoryBreakdown.Clear();
        foreach (var viewModel in breakdownItems)
        {
            CategoryBreakdown.Add(viewModel);
        }

        HasCategoryData = CategoryBreakdown.Count > 0;
        UpdateCategoryPlot(CategoryBreakdown);
    }

    private void ApplyTrend(IReadOnlyCollection<MonthlySpendingTrendPoint> data)
    {
        HasTrendData = data.Count > 0;
        UpdateTrendPlot(data);
    }

    public void RefreshTranslations()
    {
        var selectedMonth = SelectedPeriod?.Month;
        InitializePeriodOptions();
        if (selectedMonth is not null)
        {
            _isInitializingPeriods = true;
            SelectedPeriod = PeriodOptions.FirstOrDefault(option => option.Month == selectedMonth) ?? SelectedPeriod;
            _isInitializingPeriods = false;
        }

        if (_lastSummary is not null)
        {
            ApplySummary(_lastSummary);
        }

        ApplyCategoryBreakdown(_lastBreakdown);
        ApplyTrend(_lastTrend);
    }

    private static string FormatCurrency(decimal value, Currency currency)
    {
        var culture = CultureInfo.CurrentUICulture;
        return $"{value.ToString("C", culture)} ({currency})";
    }

    private static (DateOnly Start, DateOnly End) GetMonthBounds(DateOnly month)
    {
        var start = new DateOnly(month.Year, month.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return (start, end);
    }

    private void InitializePeriodOptions()
    {
        if (_isInitializingPeriods)
        {
            return;
        }

        _isInitializingPeriods = true;

        try
        {
            var previouslySelected = SelectedPeriod?.Month;
            PeriodOptions.Clear();

            var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);

            for (var offset = 0; offset < 6; offset++)
            {
                var month = currentMonth.AddMonths(-offset);
                var displayName = BuildPeriodDisplayName(month);
                PeriodOptions.Add(new ReportPeriodOptionViewModel(month, displayName));
            }

            SelectedPeriod = previouslySelected is null
                ? PeriodOptions.FirstOrDefault()
                : PeriodOptions.FirstOrDefault(option => option.Month == previouslySelected) ?? PeriodOptions.FirstOrDefault();
        }
        finally
        {
            _isInitializingPeriods = false;
        }
    }

    private void UpdateCategoryPlot(IEnumerable<ReportCategoryBreakdownViewModel> data)
    {
        var breakdown = data as IList<ReportCategoryBreakdownViewModel> ?? data.ToList();

        if (breakdown.Count == 0)
        {
            CategoryPlot = new PlotModel();
            return;
        }

        var chartPalette = ResolveChartPalette();

        var plotModel = new PlotModel
        {
            Background = chartPalette.Background,
            PlotAreaBackground = chartPalette.PlotArea,
            PlotAreaBorderColor = OxyColors.Transparent,
            TextColor = chartPalette.PrimaryText
        };

        var pieSeries = new PieSeries
        {
            InnerDiameter = 0.6,
            StrokeThickness = 0,
            OutsideLabelFormat = "{1}",
            InsideLabelFormat = "{2:0.##}%",
            FontSize = 14,
            TickDistance = 0,
            TextColor = chartPalette.PrimaryText,
            InsideLabelColor = chartPalette.PrimaryText
        };

        foreach (var item in breakdown)
        {
            var brushColor = item.AccentBrush.Color;
            var sliceColor = OxyColor.FromArgb(brushColor.A, brushColor.R, brushColor.G, brushColor.B);
            pieSeries.Slices.Add(new PieSlice(item.CategoryName, (double)item.TotalAmount)
            {
                Fill = sliceColor
            });
        }

        plotModel.Series.Clear();
        plotModel.Series.Add(pieSeries);
        plotModel.InvalidatePlot(true);

        CategoryPlot = plotModel;
    }

    private void UpdateTrendPlot(IReadOnlyCollection<MonthlySpendingTrendPoint> data)
    {
        if (data.Count == 0)
        {
            TrendPlot = new PlotModel();
            return;
        }

        var trendPoints = data as IList<MonthlySpendingTrendPoint> ?? data.OrderBy(point => point.Month).ToList();

        var chartPalette = ResolveChartPalette();

        var plotModel = new PlotModel
        {
            Background = chartPalette.Background,
            PlotAreaBackground = chartPalette.PlotArea,
            PlotAreaBorderColor = OxyColors.Transparent,
            TextColor = chartPalette.PrimaryText
        };

        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            TextColor = chartPalette.SecondaryText,
            AxislineColor = OxyColors.Transparent,
            TicklineColor = OxyColors.Transparent,
            MinorTicklineColor = OxyColors.Transparent,
            MajorGridlineStyle = LineStyle.None,
            MinorGridlineStyle = LineStyle.None
        };

        foreach (var item in trendPoints)
        {
            categoryAxis.Labels.Add(GetShortMonthLabel(item.Month));
        }

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            TextColor = chartPalette.SecondaryText,
            MinorGridlineStyle = LineStyle.None,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = chartPalette.Gridline,
            StringFormat = "C",
            Minimum = 0,
            MaximumPadding = 0.1,
            AxislineColor = OxyColors.Transparent,
            TicklineColor = OxyColors.Transparent,
            MinorTicklineColor = OxyColors.Transparent
        };

        var columnSeries = new ColumnSeries
        {
            FillColor = chartPalette.Accent,
            LabelPlacement = LabelPlacement.Inside,
            LabelFormatString = "{0:C}",
            TextColor = chartPalette.PrimaryText,
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0
        };

        foreach (var item in trendPoints)
        {
            columnSeries.Items.Add(new ColumnItem((double)item.TotalAmount));
        }

        plotModel.Axes.Add(categoryAxis);
        plotModel.Axes.Add(valueAxis);
        plotModel.Series.Add(columnSeries);
        plotModel.InvalidatePlot(true);

        TrendPlot = plotModel;
    }

    private static ChartPalette ResolveChartPalette()
    {
        var background = GetThemeColor("ChartBackgroundBrush", DefaultChartBackgroundColor);
        var plotArea = GetThemeColor("ChartPlotAreaBrush", DefaultChartPlotAreaColor);
        var gridline = GetThemeColor("ChartGridlineBrush", DefaultChartGridlineColor);
        var primaryText = GetThemeColor("PrimaryTextBrush", DefaultPrimaryTextColor);
        var secondaryText = GetThemeColor("SecondaryTextBrush", DefaultSecondaryTextColor);
        var accent = GetThemeColor("AccentBrush", DefaultAccentColor);

    return new ChartPalette(background, plotArea, gridline, primaryText, secondaryText, accent);
    }

    private static OxyColor GetThemeColor(string resourceKey, OxyColor fallback)
    {
    if (WpfApplication.Current is { } app)
        {
            var resource = app.TryFindResource(resourceKey);
            switch (resource)
            {
                case System.Windows.Media.SolidColorBrush brush:
                    return OxyColor.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
                case System.Windows.Media.Color color:
                    return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
            }
        }

        return fallback;
    }

    private readonly record struct ChartPalette(
        OxyColor Background,
        OxyColor PlotArea,
        OxyColor Gridline,
        OxyColor PrimaryText,
        OxyColor SecondaryText,
        OxyColor Accent);

    private async Task ExportAsync(ReportExportFormat format, CancellationToken cancellationToken)
    {
        if (_userId is null || SelectedPeriod is null)
        {
            return;
        }

        var selectedMonth = SelectedPeriod.Month;
        var (monthStart, monthEnd) = GetMonthBounds(selectedMonth);

        var summary = await _reportingService.GetMonthlySummaryAsync(_userId.Value, selectedMonth, cancellationToken);
        var categories = await _reportingService.GetSpendingByCategoryAsync(_userId.Value, monthStart, monthEnd, cancellationToken);
        var localizedCategories = categories
            .Select(item => item with { CategoryName = CategoryLocalization.TranslateName(_localization, item.CategoryName) })
            .ToList();
        var trendStart = selectedMonth.AddMonths(-5);
        var trend = await _reportingService.GetMonthlySpendingTrendAsync(_userId.Value, trendStart, selectedMonth, cancellationToken);

        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = format == ReportExportFormat.Pdf ? ".pdf" : ".csv",
            Filter = format == ReportExportFormat.Pdf ? "PDF (*.pdf)|*.pdf" : "CSV (*.csv)|*.csv",
            FileName = $"ExpenseReport-{selectedMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture)}"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var localization = BuildLocalization();

        try
        {
            if (format == ReportExportFormat.Pdf)
            {
                await ExportToPdfAsync(dialog.FileName, summary, localizedCategories, trend, SelectedPeriod.DisplayName, localization, cancellationToken);
            }
            else
            {
                await ExportToCsvAsync(dialog.FileName, summary, localizedCategories, trend, SelectedPeriod.DisplayName, localization, cancellationToken);
            }

            _interactionService.ShowInformation(localization.Title, Translate("INFO_EXPORT_COMPLETE"));
        }
        catch
        {
            _interactionService.ShowInformation(localization.Title, Translate("ERROR_EXPORT_FAILED"));
        }
    }

    private ReportLocalization BuildLocalization()
    {
        return new ReportLocalization(
            Translate("NAVIGATION_REPORTS"),
            Translate("REPORTS_SECTION_SUMMARY"),
            Translate("REPORTS_SECTION_CATEGORY_DISTRIBUTION"),
            Translate("REPORTS_SECTION_MONTHLY_TREND"),
            Translate("REPORTS_METRIC_TOTAL_EXPENSES"),
            Translate("REPORTS_METRIC_AVERAGE_EXPENSE"),
            Translate("REPORTS_METRIC_LARGEST_EXPENSE"),
            Translate("LABEL_MONTHLY_BUDGET"),
            Translate("REPORTS_METRIC_REMAINING"),
            Translate("REPORTS_METRIC_EXPENSE_COUNT"),
            Translate("LABEL_EXPENSES_LOWER"),
            Translate("STATUS_NO_DATA"),
            Translate("LABEL_CATEGORY"),
            Translate("REPORTS_COLUMN_EXPENSE_COUNT"),
            Translate("LABEL_AMOUNT"),
            Translate("LABEL_MONTH"),
            Translate("LABEL_TOTAL"),
            Translate("LABEL_PERIOD")
        );
    }

    private static async Task ExportToCsvAsync(
        string filePath,
        MonthlyReportSummary summary,
        IReadOnlyCollection<SpendingByCategoryReportItem> categories,
        IReadOnlyCollection<MonthlySpendingTrendPoint> trend,
        string periodDisplay,
        ReportLocalization localization,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"{localization.Title},{periodDisplay}");
        builder.AppendLine(localization.SummaryTitle);
        builder.AppendLine($"{localization.PeriodLabel},{periodDisplay}");
        builder.AppendLine($"{localization.TotalExpensesLabel},{summary.TotalExpenses.ToString("F2", CultureInfo.InvariantCulture)} {summary.Currency}");
        builder.AppendLine($"{localization.AverageExpenseLabel},{summary.AverageExpense.ToString("F2", CultureInfo.InvariantCulture)} {summary.Currency}");
        builder.AppendLine($"{localization.LargestExpenseLabel},{summary.LargestExpense.ToString("F2", CultureInfo.InvariantCulture)} {summary.Currency}");
        builder.AppendLine($"{localization.BudgetLabel},{summary.MonthlyBudget.ToString("F2", CultureInfo.InvariantCulture)} {summary.Currency}");
        builder.AppendLine($"{localization.BudgetRemainingLabel},{summary.BudgetRemaining.ToString("F2", CultureInfo.InvariantCulture)} {summary.Currency}");
        builder.AppendLine($"{localization.ExpenseCountLabel},{summary.ExpenseCount}");

        builder.AppendLine();
        builder.AppendLine(localization.CategoryTitle);
        if (categories.Count > 0)
        {
            builder.AppendLine($"{localization.CategoryColumnLabel},{localization.AmountColumnLabel},{localization.CountColumnLabel}");
            foreach (var category in categories)
            {
                builder.AppendLine($"\"{category.CategoryName}\",{category.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)} {category.Currency},{category.ExpenseCount}");
            }
        }
        else
        {
            builder.AppendLine(localization.NoDataText);
        }

        builder.AppendLine();
        builder.AppendLine(localization.TrendTitle);
        if (trend.Count > 0)
        {
            builder.AppendLine($"{localization.MonthColumnLabel},{localization.TotalColumnLabel}");
            foreach (var point in trend.OrderBy(item => item.Month))
            {
                builder.AppendLine($"{point.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture)},{point.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)} {point.Currency}");
            }
        }
        else
        {
            builder.AppendLine(localization.NoDataText);
        }

        await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken);
    }

    private static async Task ExportToPdfAsync(
        string filePath,
        MonthlyReportSummary summary,
        IReadOnlyCollection<SpendingByCategoryReportItem> categories,
        IReadOnlyCollection<MonthlySpendingTrendPoint> trend,
        string periodDisplay,
        ReportLocalization localization,
        CancellationToken cancellationToken)
    {
        EnsureQuestPdfLicense();

        var orderedCategories = categories.OrderByDescending(item => item.TotalAmount).ToList();
        var orderedTrend = trend.OrderBy(item => item.Month).ToList();

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);
                    page.Header()
                        .Text($"{localization.Title} - {periodDisplay}")
                        .FontSize(20)
                        .SemiBold();

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Text(localization.SummaryTitle).FontSize(16).SemiBold();
                        column.Item().Text($"{localization.PeriodLabel}: {periodDisplay}");
                        column.Item().Text($"{localization.TotalExpensesLabel}: {FormatCurrency(summary.TotalExpenses, summary.Currency)}");
                        column.Item().Text($"{localization.AverageExpenseLabel}: {FormatCurrency(summary.AverageExpense, summary.Currency)}");
                        column.Item().Text($"{localization.LargestExpenseLabel}: {FormatCurrency(summary.LargestExpense, summary.Currency)}");
                        column.Item().Text($"{localization.BudgetLabel}: {FormatCurrency(summary.MonthlyBudget, summary.Currency)}");
                        column.Item().Text($"{localization.BudgetRemainingLabel}: {FormatCurrency(summary.BudgetRemaining, summary.Currency)}");
                        column.Item().Text($"{localization.ExpenseCountLabel}: {summary.ExpenseCount} {localization.ExpenseSuffix}");

                        column.Item().Text(localization.CategoryTitle).FontSize(16).SemiBold();
                        if (orderedCategories.Count == 0)
                        {
                            column.Item().Text(localization.NoDataText);
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(120);
                                    columns.ConstantColumn(120);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text(localization.CategoryColumnLabel).SemiBold();
                                    header.Cell().Text(localization.AmountColumnLabel).SemiBold();
                                    header.Cell().Text(localization.CountColumnLabel).SemiBold();
                                });

                                foreach (var category in orderedCategories)
                                {
                                    table.Cell().Text(category.CategoryName);
                                    table.Cell().Text(FormatCurrency(category.TotalAmount, category.Currency));
                                    table.Cell().Text($"{category.ExpenseCount} {localization.ExpenseSuffix}");
                                }
                            });
                        }

                        column.Item().Text(localization.TrendTitle).FontSize(16).SemiBold();
                        if (orderedTrend.Count == 0)
                        {
                            column.Item().Text(localization.NoDataText);
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(140);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text(localization.MonthColumnLabel).SemiBold();
                                    header.Cell().Text(localization.TotalColumnLabel).SemiBold();
                                });

                                foreach (var point in orderedTrend)
                                {
                                    table.Cell().Text(BuildPeriodDisplayName(point.Month));
                                    table.Cell().Text(FormatCurrency(point.TotalAmount, point.Currency));
                                }
                            });
                        }
                    });
                });
            }).GeneratePdf(filePath);
        }, cancellationToken);
    }

    private static string BuildPeriodDisplayName(DateOnly month)
    {
        var monthText = GetFullMonthLabel(month);
        var format = TranslationSource.Instance["FORMAT_MONTH_YEAR"];

        if (string.Equals(format, "FORMAT_MONTH_YEAR", StringComparison.Ordinal))
        {
            format = "{0} {1}";
        }

        return string.Format(CultureInfo.CurrentUICulture, format, monthText, month.Year);
    }

    private static string GetShortMonthLabel(DateOnly month)
    {
        var monthKey = $"MONTH_SHORT_{month.Month:D2}";
        var translated = TranslationSource.Instance[monthKey];

        if (string.Equals(translated, monthKey, StringComparison.Ordinal))
        {
            var sample = new DateOnly(2000, month.Month, 1);
            translated = sample.ToDateTime(TimeOnly.MinValue).ToString("MMM", CultureInfo.CurrentUICulture);
        }

        return translated;
    }

    private static string GetFullMonthLabel(DateOnly month)
    {
        var monthKey = $"MONTH_FULL_{month.Month:D2}";
        var translated = TranslationSource.Instance[monthKey];

        if (string.Equals(translated, monthKey, StringComparison.Ordinal))
        {
            var sample = new DateOnly(2000, month.Month, 1);
            translated = sample.ToDateTime(TimeOnly.MinValue).ToString("MMMM", CultureInfo.CurrentUICulture);
        }

        return translated;
    }

    private string Translate(string key) => _localization.GetString(key);

    private void OnSelectedPeriodChanged(ReportPeriodOptionViewModel? value)
    {
        if (_isInitializingPeriods || value is null)
        {
            return;
        }

        if (RefreshCommand.CanExecute(null))
        {
            _ = RefreshCommand.ExecuteAsync(null);
        }
    }

    private void OnTranslationSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || string.Equals(e.PropertyName, "Item[]", StringComparison.Ordinal))
        {
            RefreshTranslations();
        }
    }

    private static void EnsureQuestPdfLicense()
    {
        if (_questPdfLicenseApplied)
        {
            return;
        }

    Settings.License = LicenseType.Community;
        _questPdfLicenseApplied = true;
    }

    private sealed record ReportLocalization(
        string Title,
        string SummaryTitle,
        string CategoryTitle,
        string TrendTitle,
        string TotalExpensesLabel,
        string AverageExpenseLabel,
        string LargestExpenseLabel,
        string BudgetLabel,
        string BudgetRemainingLabel,
        string ExpenseCountLabel,
        string ExpenseSuffix,
        string NoDataText,
        string CategoryColumnLabel,
        string CountColumnLabel,
        string AmountColumnLabel,
        string MonthColumnLabel,
        string TotalColumnLabel,
        string PeriodLabel);

    private enum ReportExportFormat
    {
        Pdf,
        Csv
    }

    private static bool _questPdfLicenseApplied;
}
