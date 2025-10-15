using System.Windows;
using System.Windows.Controls;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.ViewModels.Dialogs;

namespace ExpenseManager.Desktop.Views.Dialogs;

public partial class RegisterUserDialog : Window
{
    public RegisterUserDialog()
    {
        InitializeComponent();
        ViewModel = new RegisterUserDialogViewModel();
        DataContext = ViewModel;
    }

    public RegisterUserDialogViewModel ViewModel { get; }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        ViewModel.Password = passwordBox.Password;
    }

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        ViewModel.ConfirmPassword = passwordBox.Password;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.Validate(out var message))
        {
            MessageBox.Show(this, message, TranslationSource.Instance["REGISTER_DIALOG_TITLE"], MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }
}
