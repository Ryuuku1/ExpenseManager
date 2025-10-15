namespace ExpenseManager.Application.ReceiptExpenseLinks.Responses;

public sealed record ConnectionsSummaryResponse(
    int TotalExpenses,
    int LinkedExpenses,
    int UnlinkedExpenses,
    decimal ExpenseCoveragePercent,
    int TotalReceipts,
    int LinkedReceipts,
    int UnlinkedReceipts,
    decimal ReceiptCoveragePercent,
    int TotalLinks);
