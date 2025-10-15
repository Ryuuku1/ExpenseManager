namespace ExpenseManager.Desktop.ViewModels;

public sealed class TrendPointViewModel
{
    public TrendPointViewModel(string label, string formattedAmount, double rawAmount)
    {
        Label = label;
        FormattedAmount = formattedAmount;
        RawAmount = rawAmount;
    }

    public string Label { get; }
    public string FormattedAmount { get; }
    public double RawAmount { get; }
}
