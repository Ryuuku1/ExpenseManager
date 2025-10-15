namespace ExpenseManager.Application.ReceiptExpenseLinks.Responses;

public sealed record LinkSummaryResponse(
    Guid EntityId,
    string EntityTitle,
    Guid? CategoryId,
    string? CategoryName,
    DateOnly OccursOn,
    int LinkCount,
    decimal? CommissionPercent);
