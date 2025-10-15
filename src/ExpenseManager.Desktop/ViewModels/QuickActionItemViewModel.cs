namespace ExpenseManager.Desktop.ViewModels;

public sealed class QuickActionItemViewModel
{
    public QuickActionItemViewModel(string label, string glyph, string commandKey)
    {
        Label = label;
        Glyph = glyph;
        CommandKey = commandKey;
    }

    public string Label { get; }
    public string Glyph { get; }
    public string CommandKey { get; }
}
