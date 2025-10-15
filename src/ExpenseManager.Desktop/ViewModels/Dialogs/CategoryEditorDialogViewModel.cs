using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;

namespace ExpenseManager.Desktop.ViewModels.Dialogs;

public partial class CategoryEditorDialogViewModel : ObservableObject, IDisposable
{
    private readonly ILocalizationManager _localization;
    private readonly TranslationSource _translationSource;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    public bool IsEdit { get; }

    public CategoryEditorDialogViewModel(bool isEdit, ILocalizationManager localization)
    {
        IsEdit = isEdit;
        _localization = localization;
        _translationSource = TranslationSource.Instance;
        _translationSource.PropertyChanged += OnTranslationChanged;
    }

    public string Title => IsEdit
        ? _localization.GetString("CATEGORY_DIALOG_TITLE_EDIT")
        : _localization.GetString("CATEGORY_DIALOG_TITLE_NEW");

    private void OnTranslationChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Title));
    }

    public void Dispose()
    {
        _translationSource.PropertyChanged -= OnTranslationChanged;
    }
}
