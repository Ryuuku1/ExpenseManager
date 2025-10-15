using System;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class CategoryItemViewModel
{
    public CategoryItemViewModel(
        Guid id,
        string name,
        string? description,
        bool isDefault,
        int expenseCount,
        string displayName,
        string? displayDescription,
        string expenseCountText,
        string defaultBadgeText)
    {
        Id = id;
        Name = name;
        Description = description;
        IsDefault = isDefault;
        ExpenseCount = expenseCount;
        DisplayName = displayName;
        DisplayDescription = displayDescription;
        ExpenseCountText = expenseCountText;
        DefaultBadgeText = defaultBadgeText;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public bool IsDefault { get; }
    public int ExpenseCount { get; }
    public string DisplayName { get; }
    public string? DisplayDescription { get; }
    public string ExpenseCountText { get; }
    public string DefaultBadgeText { get; }
}
