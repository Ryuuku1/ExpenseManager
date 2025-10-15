using System.Windows;
using ExpenseManager.Desktop.Localization;

namespace ExpenseManager.Desktop.Services;

internal sealed class UserInteractionService : IUserInteractionService
{
    private readonly ILocalizationManager _localizationManager;

    public UserInteractionService(ILocalizationManager localizationManager)
    {
        _localizationManager = localizationManager;
    }

    public void ShowInformation(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowFeatureComingSoon(string featureName)
    {
        var title = _localizationManager.GetString("TITLE_FEATURE_IN_DEVELOPMENT");
        var message = _localizationManager.GetString("MESSAGE_FEATURE_COMING_SOON", featureName);
        ShowInformation(title, message);
    }

    public bool Confirm(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public RecurringEventDeletionChoice ConfirmRecurringDeletion(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return result switch
        {
            MessageBoxResult.Yes => RecurringEventDeletionChoice.SingleOccurrence,
            MessageBoxResult.No => RecurringEventDeletionChoice.EntireSeries,
            _ => RecurringEventDeletionChoice.Cancel
        };
    }
}
