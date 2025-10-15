using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;

namespace ExpenseManager.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUserSessionService _sessionService;
    private readonly IUserInteractionService _interactionService;
    private readonly ILocalizationManager _localization;

    public event EventHandler? LoginSucceeded;

    public event EventHandler? LoginCancelled;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel(IUserSessionService sessionService, IUserInteractionService interactionService, ILocalizationManager localization)
    {
        _sessionService = sessionService;
        _interactionService = interactionService;
        _localization = localization;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            _interactionService.ShowInformation(Translate("TITLE_AUTHENTICATION"), Translate("MESSAGE_ENTER_CREDENTIALS"));
            return;
        }

        try
        {
            IsBusy = true;
            var success = await _sessionService.AuthenticateAsync(UserName, Password, CancellationToken.None);
            if (!success)
            {
                _interactionService.ShowInformation(Translate("TITLE_AUTHENTICATION"), Translate("ERROR_INVALID_CREDENTIALS"));
                return;
            }

            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        LoginCancelled?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> RegisterAsync(string userName, string password, string? displayName)
    {
        if (IsBusy)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            _interactionService.ShowInformation(Translate("TITLE_AUTHENTICATION"), Translate("MESSAGE_FILL_CREDENTIALS"));
            return false;
        }

        try
        {
            IsBusy = true;
            await _sessionService.RegisterAsync(userName, password, displayName, CancellationToken.None);
            _interactionService.ShowInformation(Translate("TITLE_AUTHENTICATION"), Translate("INFO_ACCOUNT_CREATED"));
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("TITLE_AUTHENTICATION"), exception.Message);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string Translate(string text) => _localization.GetString(text);
}
