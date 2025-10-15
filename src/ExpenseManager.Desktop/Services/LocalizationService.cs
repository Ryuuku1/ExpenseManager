using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;

namespace ExpenseManager.Desktop.Services;

internal sealed class LocalizationService : ILocalizationService
{
    private readonly ILocalizationManager _localizationManager;
    private static bool _languageMetadataOverridden;

    public LocalizationService(ILocalizationManager localizationManager)
    {
        _localizationManager = localizationManager;
    }

    public bool TryApplyCulture(string cultureName, [NotNullWhen(false)] out string? errorMessage)
    {
        if (!_localizationManager.TrySetCulture(cultureName, out var managerError))
        {
            errorMessage = !string.IsNullOrWhiteSpace(managerError)
                ? managerError
                : _localizationManager.GetString("ERROR_LANGUAGE_INVALID");
            return false;
        }

        var culture = _localizationManager.CurrentCulture;
        errorMessage = null;

        var xmlLanguage = XmlLanguage.GetLanguage(culture.IetfLanguageTag);

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (System.Windows.Application.Current.MainWindow is not null)
            {
                System.Windows.Application.Current.MainWindow.Language = xmlLanguage;
            }

            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (!Equals(window.Language, xmlLanguage))
                {
                    window.Language = xmlLanguage;
                }
            }

            if (!_languageMetadataOverridden)
            {
                try
                {
                    FrameworkElement.LanguageProperty.OverrideMetadata(
                        typeof(FrameworkElement),
                        new FrameworkPropertyMetadata(xmlLanguage));

                    FrameworkContentElement.LanguageProperty.OverrideMetadata(
                        typeof(FrameworkContentElement),
                        new FrameworkPropertyMetadata(xmlLanguage));
                }
                catch (ArgumentException)
                {
                    // The language metadata has already been overridden by another component.
                }
                finally
                {
                    _languageMetadataOverridden = true;
                }
            }

            TranslationSource.Instance.Refresh();
        });

        return true;
    }
}
