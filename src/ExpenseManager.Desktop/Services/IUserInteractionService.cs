namespace ExpenseManager.Desktop.Services;

public interface IUserInteractionService
{
    void ShowInformation(string title, string message);

    void ShowFeatureComingSoon(string featureName);

    bool Confirm(string title, string message);

    RecurringEventDeletionChoice ConfirmRecurringDeletion(string title, string message);
}
