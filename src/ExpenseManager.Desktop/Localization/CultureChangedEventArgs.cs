using System;
using System.Globalization;

namespace ExpenseManager.Desktop.Localization;

public sealed class CultureChangedEventArgs : EventArgs
{
    public CultureChangedEventArgs(CultureInfo previousCulture, CultureInfo currentCulture)
    {
        PreviousCulture = previousCulture ?? throw new ArgumentNullException(nameof(previousCulture));
        CurrentCulture = currentCulture ?? throw new ArgumentNullException(nameof(currentCulture));
    }

    public CultureInfo PreviousCulture { get; }

    public CultureInfo CurrentCulture { get; }
}
