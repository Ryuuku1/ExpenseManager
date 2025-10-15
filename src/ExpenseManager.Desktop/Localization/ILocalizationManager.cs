using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExpenseManager.Desktop.Localization;

public interface ILocalizationManager
{
    CultureInfo CurrentCulture { get; }

    CultureInfo DefaultCulture { get; }

    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    event EventHandler<CultureChangedEventArgs> CultureChanged;

    bool TrySetCulture(string cultureName, out string? errorMessage);

    string GetString(string key);

    string GetString(string key, params object[] arguments);
}
