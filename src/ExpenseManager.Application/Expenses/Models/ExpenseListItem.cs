using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Expenses.Models;

public sealed record ExpenseListItem(
    Guid Id,
    string Title,
    string CategoryName,
    decimal Amount,
    Currency Currency,
    DateOnly ExpenseDate,
    ExpenseStatus Status,
    PaymentMethod PaymentMethod,
    DateOnly? DueDate,
    RecurrenceType Recurrence);
