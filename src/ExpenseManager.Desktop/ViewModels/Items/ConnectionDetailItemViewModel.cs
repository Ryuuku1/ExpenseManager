namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed record ConnectionDetailItemViewModel(
    Guid LinkId,
    Guid ExpenseId,
    string ExpenseTitle,
    Guid ReceiptId,
    string ReceiptTitle,
    string? Notes,
    DateTime CreatedAtLocal,
    DateTime? UpdatedAtLocal);
