using System.Windows;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.ViewModels.Dialogs;

namespace ExpenseManager.Desktop.Views.Dialogs;

public partial class AlertEditorDialog : Window
{
    public AlertEditorDialog(AlertEditorDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
    }

    public AlertEditorDialogViewModel ViewModel { get; }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.Validate(out var message))
        {
            MessageBox.Show(this, message, TranslationSource.Instance["SECTION_ALERTS"], MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }
}
