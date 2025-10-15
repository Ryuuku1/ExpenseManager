using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExpenseManager.Desktop.Converters;

public sealed class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasText = value is string text && !string.IsNullOrWhiteSpace(text);
        return hasText ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
