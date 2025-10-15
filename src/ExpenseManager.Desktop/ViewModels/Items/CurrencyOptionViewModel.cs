using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed record CurrencyOptionViewModel(Currency Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
