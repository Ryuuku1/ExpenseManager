using System;
using ExpenseManager.Domain.Abstractions;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Domain.ValueObjects;

namespace ExpenseManager.Domain.Entities.Expenses;

public sealed class RecurringExpenseTemplate : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public string? Notes { get; private set; }
    public Money Amount { get; private set; }
    public RecurrenceType Recurrence { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private RecurringExpenseTemplate()
    {
        Name = string.Empty;
        Amount = Money.Create(0, Currency.Eur);
    }

    private RecurringExpenseTemplate(Guid id, Guid userId, Guid categoryId, string name, string? notes, Money amount, RecurrenceType recurrence, PaymentMethod paymentMethod, DateOnly startDate, DateOnly? endDate) : base(id)
    {
        UserId = userId;
        CategoryId = categoryId;
        Name = name;
        Notes = notes;
        Amount = amount;
        Recurrence = recurrence;
        PaymentMethod = paymentMethod;
        StartDate = startDate;
        EndDate = endDate;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static RecurringExpenseTemplate Create(Guid userId, Guid categoryId, string name, Money amount, RecurrenceType recurrence, PaymentMethod paymentMethod, DateOnly startDate, DateOnly? endDate = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        if (recurrence == RecurrenceType.None)
        {
            throw new ArgumentException("Recurrence must be specified for recurring templates.", nameof(recurrence));
        }

        return new RecurringExpenseTemplate(Guid.NewGuid(), userId, categoryId, name.Trim(), notes?.Trim(), amount, recurrence, paymentMethod, startDate, endDate);
    }

    public void UpdateDetails(string name, string? notes, Guid categoryId, Money amount, RecurrenceType recurrence, PaymentMethod paymentMethod, DateOnly startDate, DateOnly? endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
        Notes = notes?.Trim();
        CategoryId = categoryId;
        Amount = amount;
        Recurrence = recurrence;
        PaymentMethod = paymentMethod;
        StartDate = startDate;
        EndDate = endDate;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}