using System;
using System.Globalization;
using System.Windows.Data;
using ExpenseManager.Desktop.Localization;

namespace ExpenseManager.Desktop.Converters;

public sealed class TranslationConverter : IValueConverter
{
    private readonly ILocalizationManager? _localization;

    public TranslationConverter()
    {
        _localization = App.Services?.GetService(typeof(ILocalizationManager)) as ILocalizationManager;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key || string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

    return _localization?.GetString(key) ?? key;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
