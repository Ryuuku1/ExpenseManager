namespace ExpenseManager.Application.ReceiptExpenseLinks.Requests;

public sealed record GetConnectionsSummaryRequest(
    Guid UserId,
    DateOnly? From,
    DateOnly? To,
    Guid? ExpenseCategoryId,
    Guid? ReceiptCategoryId);
