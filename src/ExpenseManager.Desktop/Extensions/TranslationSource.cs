using System;
using System.ComponentModel;
using ExpenseManager.Desktop.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseManager.Desktop.Extensions;

public sealed class TranslationSource : INotifyPropertyChanged
{
    private ILocalizationManager? _localizationManager;

    private TranslationSource()
    {
    }

    public static TranslationSource Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Initialize(IServiceProvider services)
    {
        var manager = services.GetRequiredService<ILocalizationManager>();

        if (!ReferenceEquals(_localizationManager, manager))
        {
            if (_localizationManager is not null)
            {
                _localizationManager.CultureChanged -= OnCultureChanged;
            }

            _localizationManager = manager;
            _localizationManager.CultureChanged += OnCultureChanged;
        }

        Refresh();
    }

    public string this[string key]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return _localizationManager?.GetString(key) ?? key;
        }
    }

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        Refresh();
    }
}
