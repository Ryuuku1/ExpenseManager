namespace ExpenseManager.Application.ReceiptExpenseLinks.Requests;

public sealed record DeleteReceiptExpenseLinkRequest(
    Guid UserId,
    Guid LinkId,
    Guid RequestedBy);
