namespace ExpenseManager.Desktop.Localization;

public sealed class LocalizationOptions
{
    public string DefaultCulture { get; set; } = "pt-PT";

    public string? FallbackCulture { get; set; }
        = "pt-PT";

    public string[]? SupportedCultures { get; set; }
        = null;
}
