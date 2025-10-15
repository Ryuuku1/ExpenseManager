namespace ExpenseManager.Application.ReceiptExpenseLinks.Responses;

public sealed record ReceiptExpenseLinkDetailResponse(
    Guid LinkId,
    Guid ExpenseId,
    string ExpenseTitle,
    Guid ReceiptId,
    string ReceiptTitle,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
