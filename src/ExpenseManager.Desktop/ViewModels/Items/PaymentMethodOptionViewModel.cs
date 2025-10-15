using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed record PaymentMethodOptionViewModel(PaymentMethod Value, string DisplayName);
