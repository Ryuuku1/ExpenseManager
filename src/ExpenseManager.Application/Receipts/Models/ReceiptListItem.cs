using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Receipts.Models;

public sealed record ReceiptListItem(
    Guid Id,
    string Title,
    Guid? CategoryId,
    string? CategoryName,
    DateOnly ReceiptDate,
    decimal NetAmount,
    Currency Currency,
    string? Vendor,
    string? ReferenceNumber,
    decimal? CommissionPercent);
