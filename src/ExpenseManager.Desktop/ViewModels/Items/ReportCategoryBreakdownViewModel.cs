using System.Windows.Media;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class ReportCategoryBreakdownViewModel
{
    public ReportCategoryBreakdownViewModel(string categoryName, decimal totalAmount, Currency currency, int expenseCount, SolidColorBrush accentBrush)
    {
        CategoryName = categoryName;
        TotalAmount = totalAmount;
        Currency = currency;
        ExpenseCount = expenseCount;
        AccentBrush = accentBrush;
        if (!AccentBrush.IsFrozen)
        {
            AccentBrush.Freeze();
        }
    }

    public string CategoryName { get; }
    public decimal TotalAmount { get; }
    public Currency Currency { get; }
    public int ExpenseCount { get; }
    public SolidColorBrush AccentBrush { get; }
}
