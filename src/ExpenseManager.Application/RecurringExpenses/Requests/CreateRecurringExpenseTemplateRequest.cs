using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.RecurringExpenses.Requests;

public sealed record CreateRecurringExpenseTemplateRequest(
    Guid UserId,
    Guid CategoryId,
    string Name,
    string? Notes,
    decimal Amount,
    Currency Currency,
    RecurrenceType Recurrence,
    PaymentMethod PaymentMethod,
    DateOnly StartDate,
    DateOnly? EndDate);
