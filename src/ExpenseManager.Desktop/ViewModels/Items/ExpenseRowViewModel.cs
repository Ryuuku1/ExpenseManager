using System;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class ExpenseRowViewModel
{
    public ExpenseRowViewModel(
        Guid id,
        string title,
        string categoryName,
        string amountDisplay,
        string dateDisplay,
        string statusDisplay,
        string paymentMethodDisplay,
        string dueDateDisplay)
    {
        Id = id;
        Title = title;
        CategoryName = categoryName;
        AmountDisplay = amountDisplay;
        DateDisplay = dateDisplay;
        StatusDisplay = statusDisplay;
        PaymentMethodDisplay = paymentMethodDisplay;
        DueDateDisplay = dueDateDisplay;
    }

    public Guid Id { get; }
    public string Title { get; }
    public string CategoryName { get; }
    public string AmountDisplay { get; }
    public string DateDisplay { get; }
    public string StatusDisplay { get; }
    public string PaymentMethodDisplay { get; }
    public string DueDateDisplay { get; }
}
