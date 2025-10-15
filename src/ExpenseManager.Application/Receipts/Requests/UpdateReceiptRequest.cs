using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Receipts.Requests;

public sealed record UpdateReceiptRequest(
    Guid UserId,
    Guid ReceiptId,
    Guid? CategoryId,
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
    long? FileSizeInBytes);
