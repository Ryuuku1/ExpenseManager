using System;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class ReportPeriodOptionViewModel
{
    public ReportPeriodOptionViewModel(DateOnly month, string displayName)
    {
        Month = new DateOnly(month.Year, month.Month, 1);
        DisplayName = displayName;
    }

    public DateOnly Month { get; }

    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}
