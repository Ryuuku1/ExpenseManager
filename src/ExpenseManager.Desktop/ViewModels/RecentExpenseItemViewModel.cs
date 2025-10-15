using System;

namespace ExpenseManager.Desktop.ViewModels;

public sealed class RecentExpenseItemViewModel
{
    public RecentExpenseItemViewModel(Guid expenseId, string title, string categoryName, string amount, string date)
    {
        ExpenseId = expenseId;
        Title = title;
        CategoryName = categoryName;
        Amount = amount;
        Date = date;
    }

    public Guid ExpenseId { get; }
    public string Title { get; }
    public string CategoryName { get; }
    public string Amount { get; }
    public string Date { get; }
}
