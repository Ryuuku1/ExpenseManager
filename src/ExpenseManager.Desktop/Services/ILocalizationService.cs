using System.Diagnostics.CodeAnalysis;

namespace ExpenseManager.Desktop.Services;

public interface ILocalizationService
{
    bool TryApplyCulture(string cultureName, [NotNullWhen(false)] out string? errorMessage);
}
