using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.Localization;

internal static class AlertLocalization
{
    public static string TranslateType(ILocalizationManager localization, AlertType alertType)
    {
        var key = alertType switch
        {
            AlertType.BudgetLimit => "ALERT_TYPE_BUDGET_LIMIT",
            AlertType.PaymentReminder => "ALERT_TYPE_PAYMENT_REMINDER",
            AlertType.RecurringExpense => "ALERT_TYPE_RECURRING_EXPENSE",
            AlertType.UpcomingBill => "ALERT_TYPE_UPCOMING_BILL",
            AlertType.Custom => "ALERT_TYPE_CUSTOM",
            _ => alertType.ToString().ToUpperInvariant()
        };

        return localization.GetString(key);
    }
}
