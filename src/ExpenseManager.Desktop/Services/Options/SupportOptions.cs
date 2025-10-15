namespace ExpenseManager.Desktop.Services.Options;

public sealed class SupportOptions
{
    public const string SectionName = "Support";

    public string? PayPalEmail { get; set; }

    public string? CurrencyCode { get; set; } = "EUR";
}
