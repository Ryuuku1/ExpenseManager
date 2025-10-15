using System;
using System.Threading.Tasks;
using System.Windows;
using ExpenseManager.Desktop.ViewModels;
using ExpenseManager.Desktop.Views.Dialogs;

namespace ExpenseManager.Desktop.Views.Dialogs;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LoginSucceeded += OnLoginSucceeded;
        viewModel.LoginCancelled += OnLoginCancelled;
        Closed += (_, _) =>
        {
            viewModel.LoginSucceeded -= OnLoginSucceeded;
            viewModel.LoginCancelled -= OnLoginCancelled;
        };
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel viewModel || sender is not System.Windows.Controls.PasswordBox passwordBox)
        {
            return;
        }

        viewModel.Password = passwordBox.Password;
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        DialogResult = true;
    }

    private void OnLoginCancelled(object? sender, EventArgs e)
    {
        DialogResult = false;
    }

    private async void OnRegisterClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel viewModel)
        {
            return;
        }

        var dialog = new RegisterUserDialog
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        var registered = await viewModel.RegisterAsync(dialog.ViewModel.UserName, dialog.ViewModel.Password, dialog.ViewModel.DisplayName);
        if (registered && !DialogResult.HasValue)
        {
            DialogResult = true;
        }
    }
}
