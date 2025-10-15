namespace ExpenseManager.Application.ReceiptExpenseLinks.Models;

public sealed record ReceiptConnectionInfo(
    Guid ReceiptId,
    string ReceiptTitle,
    Guid? CategoryId,
    string? CategoryName,
    DateOnly ReceiptDate,
    int LinkCount,
    decimal? CommissionPercent);
