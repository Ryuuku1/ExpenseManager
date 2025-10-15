using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ExpenseManager.Desktop.Extensions;

namespace ExpenseManager.Desktop.ViewModels.Dialogs;

public sealed partial class RegisterUserDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    public bool Validate(out string? message)
    {
        message = null;

        if (string.IsNullOrWhiteSpace(UserName))
        {
            message = Translate("ERROR_REGISTRATION_USERNAME_REQUIRED");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            message = Translate("ERROR_REGISTRATION_PASSWORD_REQUIRED");
            return false;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            message = Translate("ERROR_REGISTRATION_PASSWORD_MISMATCH");
            return false;
        }

        return true;
    }

    private static string Translate(string key) => TranslationSource.Instance[key];
}
