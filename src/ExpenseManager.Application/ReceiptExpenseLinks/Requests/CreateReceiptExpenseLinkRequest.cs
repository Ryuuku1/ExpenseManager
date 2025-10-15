namespace ExpenseManager.Application.ReceiptExpenseLinks.Requests;

public sealed record CreateReceiptExpenseLinkRequest(
    Guid UserId,
    Guid ExpenseId,
    Guid ReceiptId,
    Guid RequestedBy,
    string? Notes);
