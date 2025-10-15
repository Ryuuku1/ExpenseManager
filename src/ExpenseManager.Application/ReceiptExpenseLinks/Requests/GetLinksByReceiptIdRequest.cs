namespace ExpenseManager.Application.ReceiptExpenseLinks.Requests;

public sealed record GetLinksByReceiptIdRequest(
    Guid UserId,
    Guid ReceiptId,
    DateOnly? From,
    DateOnly? To,
    Guid? ExpenseCategoryId);
