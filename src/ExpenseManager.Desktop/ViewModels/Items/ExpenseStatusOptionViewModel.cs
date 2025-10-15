using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed record ExpenseStatusOptionViewModel(ExpenseStatus Value, string DisplayName);
