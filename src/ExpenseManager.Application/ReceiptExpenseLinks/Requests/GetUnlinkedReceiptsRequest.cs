namespace ExpenseManager.Application.ReceiptExpenseLinks.Requests;

public sealed record GetUnlinkedReceiptsRequest(
    Guid UserId,
    DateOnly? From,
    DateOnly? To,
    Guid? CategoryId);
