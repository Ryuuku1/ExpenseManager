using System;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed record CategoryOptionViewModel(Guid Id, string Name, string DisplayName)
{
	public override string ToString() => DisplayName;
}
