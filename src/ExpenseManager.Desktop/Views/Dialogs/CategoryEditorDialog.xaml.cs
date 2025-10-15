using System;
using System.ComponentModel;
using System.Windows;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication = System.Windows.Application;

namespace ExpenseManager.Desktop.Views.Dialogs;

public partial class CategoryEditorDialog : Window
{
    private readonly ILocalizationManager _localization;

    public CategoryEditorDialogViewModel ViewModel { get; }

    public CategoryEditorDialog(string? name = null, string? description = null)
    {
        var resourceLocator = new Uri("/ExpenseManager.Desktop;component/Views/Dialogs/CategoryEditorDialog.xaml", UriKind.Relative);
        WpfApplication.LoadComponent(this, resourceLocator);

    var services = App.Services ?? throw new InvalidOperationException("Application services are not initialized.");
    _localization = services.GetRequiredService<ILocalizationManager>();

        ViewModel = new CategoryEditorDialogViewModel(!string.IsNullOrWhiteSpace(name), _localization);
        if (!string.IsNullOrWhiteSpace(name))
        {
            ViewModel.Name = CategoryLocalization.TranslateName(_localization, name);
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            ViewModel.Description = CategoryLocalization.TranslateDescription(_localization, description);
        }
        DataContext = ViewModel;
        Title = ViewModel.Title;

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += OnClosed;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Name))
        {
            MessageBox.Show(
                this,
                _localization.GetString("ERROR_CATEGORY_NAME_REQUIRED"),
                _localization.GetString("NAVIGATION_CATEGORIES"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(ViewModel.Title), StringComparison.Ordinal))
        {
            Title = ViewModel.Title;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        Closed -= OnClosed;
    }
}
