namespace ExpenseManager.Application.ReceiptExpenseLinks.Requests;

public sealed record GetLinksByExpenseIdRequest(
    Guid UserId,
    Guid ExpenseId,
    DateOnly? From,
    DateOnly? To,
    Guid? ReceiptCategoryId);
