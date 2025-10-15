using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Receipts.Models;

public sealed record ReceiptDetails(
    Guid Id,
    Guid UserId,
    Guid? CategoryId,
    string? CategoryName,
    string Title,
    string? Description,
    string? ReferenceNumber,
    string? Vendor,
    DateOnly ReceiptDate,
    decimal NetAmount,
    Currency Currency,
    decimal? CommissionPercent,
    string? FileName,
    string? FilePath,
    long? FileSizeInBytes,
    DateTime CapturedAt,
    DateTime UpdatedAt);
