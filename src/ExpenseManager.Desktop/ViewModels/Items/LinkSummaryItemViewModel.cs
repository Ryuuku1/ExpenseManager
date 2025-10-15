namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed record LinkSummaryItemViewModel(
    Guid EntityId,
    string Title,
    string CategoryDisplay,
    string DateDisplay,
    string LinkCountDisplay,
    int LinkCount,
    DateOnly OccursOn,
    Guid? CategoryId,
    decimal? CommissionPercent,
    string? ExtraDisplay);
