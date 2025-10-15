using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace ExpenseManager.Desktop.Extensions;

[MarkupExtensionReturnType(typeof(string))]
public sealed class TranslateExtension : MarkupExtension
{
    public TranslateExtension()
    {
    }

    public TranslateExtension(string text)
    {
        Text = text;
    }

    public string? Text { get; set; }

    public object[]? Arguments { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return string.Empty;
        }

        var binding = new Binding($"[{Text}]")
        {
            Source = TranslationSource.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
