using System;

namespace ExpenseManager.Application.Receipts.Models;

public sealed record ExpenseReceiptItem(
    Guid Id,
    Guid ExpenseId,
    string ExpenseTitle,
    string FileName,
    string FilePath,
    long FileSizeInBytes,
    DateTime UploadedAt);
