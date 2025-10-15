using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class ExpenseListItemViewModel
{
    public ExpenseListItemViewModel(Guid id, string title, string categoryName, decimal amount, Currency currency, DateOnly expenseDate, ExpenseStatus status, PaymentMethod paymentMethod, DateOnly? dueDate, RecurrenceType recurrence)
    {
        Id = id;
        Title = title;
        CategoryName = categoryName;
        Amount = amount;
        Currency = currency;
        ExpenseDate = expenseDate;
        Status = status;
        PaymentMethod = paymentMethod;
        DueDate = dueDate;
        Recurrence = recurrence;
    }

    public Guid Id { get; }
    public string Title { get; }
    public string CategoryName { get; }
    public decimal Amount { get; }
    public Currency Currency { get; }
    public DateOnly ExpenseDate { get; }
    public ExpenseStatus Status { get; }
    public PaymentMethod PaymentMethod { get; }
    public DateOnly? DueDate { get; }
    public RecurrenceType Recurrence { get; }

    public string FormattedAmount => $"{Amount:N2} {Currency}";

    public string FormattedExpenseDate => ExpenseDate.ToString("dd/MM/yyyy");

    public string FormattedDueDate => DueDate?.ToString("dd/MM/yyyy") ?? "-";

    public string StatusDisplay => Status.ToString();

    public string PaymentMethodDisplay => PaymentMethod.ToString();

    public string RecurrenceDisplay => Recurrence == RecurrenceType.None ? "Sem recorrência" : Recurrence.ToString();

    public override string ToString()
    {
        return $"{Title} - {Amount:N2} {Currency}";
    }
}
