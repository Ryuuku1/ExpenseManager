using System;
using System.Collections.Generic;
using ExpenseManager.Application.Expenses.Requests.Shared;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Expenses.Requests;

public sealed record CreateExpenseRequest(
    Guid UserId,
    Guid CategoryId,
    string Title,
    string? Description,
    decimal Amount,
    Currency Currency,
    PaymentMethod PaymentMethod,
    DateOnly ExpenseDate,
    DateOnly? DueDate,
    RecurrenceType Recurrence,
    IReadOnlyCollection<CreateExpenseAttachmentRequest> Attachments);
