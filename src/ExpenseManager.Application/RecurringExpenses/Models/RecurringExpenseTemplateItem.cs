using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.RecurringExpenses.Models;

public sealed record RecurringExpenseTemplateItem(
    Guid Id,
    string Name,
    string CategoryName,
    decimal Amount,
    Currency Currency,
    RecurrenceType Recurrence,
    PaymentMethod PaymentMethod,
    DateOnly StartDate,
    DateOnly? EndDate);
