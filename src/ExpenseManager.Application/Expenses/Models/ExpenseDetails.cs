using System;
using System.Collections.Generic;
using ExpenseManager.Application.Expenses.Models.Shared;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Expenses.Models;

public sealed record ExpenseDetails(
    Guid Id,
    string Title,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    Currency Currency,
    PaymentMethod PaymentMethod,
    ExpenseStatus Status,
    DateOnly ExpenseDate,
    DateOnly? DueDate,
    RecurrenceType Recurrence,
    IReadOnlyCollection<ExpenseAttachmentItem> Attachments);
