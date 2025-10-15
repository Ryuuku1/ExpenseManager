namespace ExpenseManager.Application.ReceiptExpenseLinks.Requests;

public sealed record UpdateReceiptExpenseLinkRequest(
    Guid UserId,
    Guid LinkId,
    Guid ExpenseId,
    Guid ReceiptId,
    Guid RequestedBy,
    string? Notes);
