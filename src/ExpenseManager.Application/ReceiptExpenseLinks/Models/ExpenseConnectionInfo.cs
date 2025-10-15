namespace ExpenseManager.Application.ReceiptExpenseLinks.Models;

public sealed record ExpenseConnectionInfo(
    Guid ExpenseId,
    string ExpenseTitle,
    Guid? CategoryId,
    string? CategoryName,
    DateOnly ExpenseDate,
    int LinkCount);
