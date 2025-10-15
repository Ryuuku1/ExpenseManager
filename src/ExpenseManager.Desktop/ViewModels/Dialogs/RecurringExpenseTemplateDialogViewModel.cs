using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ExpenseManager.Application.RecurringExpenses.Requests;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Domain.Enumerations;
using CategoryOptionViewModel = ExpenseManager.Desktop.ViewModels.Items.CategoryOptionViewModel;
using PaymentMethodOptionViewModel = ExpenseManager.Desktop.ViewModels.Items.PaymentMethodOptionViewModel;

namespace ExpenseManager.Desktop.ViewModels.Dialogs;

public sealed partial class RecurringExpenseTemplateDialogViewModel : ObservableObject
{
    private readonly ILocalizationManager _localization;

    public RecurringExpenseTemplateDialogViewModel(ILocalizationManager localization)
    {
        _localization = localization;
        PopulatePaymentMethods();
    }

    public ObservableCollection<CategoryOptionViewModel> Categories { get; } = new();
    public ObservableCollection<PaymentMethodOptionViewModel> PaymentMethods { get; } = new();

    [ObservableProperty]
    private CategoryOptionViewModel? _selectedCategory;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private Currency _selectedCurrency = Currency.Eur;

    [ObservableProperty]
    private RecurrenceType _selectedRecurrence = RecurrenceType.Monthly;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.DebitCard;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private bool _hasEndDate;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    public Array CurrencyValues { get; } = Enum.GetValues(typeof(Currency));

    public Array RecurrenceValues { get; } = Enum.GetValues(typeof(RecurrenceType));

    public string DialogTitle => Translate("RECURRING_TEMPLATE_DIALOG_TITLE");

    public void InitializeCategories(IEnumerable<CategoryOptionViewModel> categories)
    {
        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }

        SelectedCategory = Categories.Count > 0 ? Categories[0] : null;
    }

    public bool Validate(out string? message)
    {
        message = null;

        if (SelectedCategory is null)
        {
            message = Translate("ERROR_SELECT_CATEGORY");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            message = Translate("ERROR_TEMPLATE_NAME_REQUIRED");
            return false;
        }

        if (Amount <= 0)
        {
            message = Translate("ERROR_AMOUNT_GREATER_ZERO");
            return false;
        }

        if (SelectedRecurrence == RecurrenceType.None)
        {
            message = Translate("ERROR_RECURRENCE_REQUIRED");
            return false;
        }

        if (HasEndDate && EndDate.Date < StartDate.Date)
        {
            message = Translate("ERROR_END_DATE_BEFORE_START");
            return false;
        }

        return true;
    }

    public CreateRecurringExpenseTemplateRequest ToRequest(Guid userId)
    {
        if (SelectedCategory is null)
        {
            throw new InvalidOperationException(Translate("ERROR_CATEGORY_NOT_SELECTED"));
        }

        var startDateOnly = DateOnly.FromDateTime(StartDate.Date);
        DateOnly? endDate = HasEndDate ? DateOnly.FromDateTime(EndDate.Date) : null;

        return new CreateRecurringExpenseTemplateRequest(
            userId,
            SelectedCategory.Id,
            Name.Trim(),
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            Amount,
            SelectedCurrency,
            SelectedRecurrence,
            SelectedPaymentMethod,
            startDateOnly,
            endDate);
    }

    private string Translate(string key) => _localization.GetString(key);

    public void RefreshPaymentMethods()
    {
        PopulatePaymentMethods();
    }

    private void PopulatePaymentMethods()
    {
        var previous = SelectedPaymentMethod;

        PaymentMethods.Clear();
        foreach (var method in Enum.GetValues<PaymentMethod>())
        {
            var displayName = TranslatePaymentMethod(method);
            PaymentMethods.Add(new PaymentMethodOptionViewModel(method, displayName));
        }

        if (PaymentMethods.Count == 0)
        {
            return;
        }

        if (PaymentMethods.Any(option => option.Value == previous))
        {
            SelectedPaymentMethod = previous;
        }
        else
        {
            SelectedPaymentMethod = PaymentMethods[0].Value;
        }
    }

    private string TranslatePaymentMethod(PaymentMethod method)
    {
    var key = $"PAYMENT_METHOD_{method.ToString().ToUpperInvariant()}";
    var value = _localization.GetString(key);
        return string.Equals(value, key, StringComparison.Ordinal) ? method.ToString() : value;
    }
}
