namespace ExpenseManager.Application.ReceiptExpenseLinks.Requests;

public sealed record GetUnlinkedExpensesRequest(
    Guid UserId,
    DateOnly? From,
    DateOnly? To,
    Guid? CategoryId,
    bool OnlyOpenExpenses);
