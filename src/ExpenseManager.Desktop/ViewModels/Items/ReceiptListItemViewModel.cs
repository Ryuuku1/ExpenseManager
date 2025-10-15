namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed record ReceiptListItemViewModel(
    Guid Id,
    string Title,
    string CategoryDisplay,
    string AmountDisplay,
    string DateDisplay,
    string? Vendor,
    string? ReferenceNumber,
    string? CommissionDisplay);
