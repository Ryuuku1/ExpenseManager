namespace ExpenseManager.Desktop.ViewModels;

public sealed class SummaryCardItemViewModel
{
    public SummaryCardItemViewModel(string title, string value, string glyph, string semantic)
    {
        Title = title;
        Value = value;
        Glyph = glyph;
        Semantic = semantic;
    }

    public string Title { get; }
    public string Value { get; }
    public string Glyph { get; }
    public string Semantic { get; }
}
