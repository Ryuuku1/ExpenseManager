using ExpenseManager.Desktop.Services.Branding;

namespace ExpenseManager.Desktop.Services.Options;

public sealed class BrandingOptions
{
    public const string SectionName = "Branding";

    public string? LogoPath { get; init; }

    public string? IconPath { get; init; }

    public BrandingColorScheme ColorScheme { get; init; } = BrandingColorScheme.Midnight;
}
