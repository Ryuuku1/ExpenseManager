using System;
using System.Collections.ObjectModel;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class ExpenseDetailsViewModel
{
    public ExpenseDetailsViewModel(
        Guid id,
        string title,
        string? description,
        string categoryName,
        string amountDisplay,
        string statusDisplay,
        string paymentMethodDisplay,
        string expenseDateDisplay,
        string? dueDateDisplay)
    {
        Id = id;
        Title = title;
        Description = description;
        CategoryName = categoryName;
        AmountDisplay = amountDisplay;
        StatusDisplay = statusDisplay;
        PaymentMethodDisplay = paymentMethodDisplay;
        ExpenseDateDisplay = expenseDateDisplay;
        DueDateDisplay = dueDateDisplay;
    }

    public Guid Id { get; }
    public string Title { get; }
    public string? Description { get; }
    public string CategoryName { get; }
    public string AmountDisplay { get; }
    public string StatusDisplay { get; }
    public string PaymentMethodDisplay { get; }
    public string ExpenseDateDisplay { get; }
    public string? DueDateDisplay { get; }

    public ObservableCollection<ExpenseAttachmentItemViewModel> Attachments { get; } = new();
}
