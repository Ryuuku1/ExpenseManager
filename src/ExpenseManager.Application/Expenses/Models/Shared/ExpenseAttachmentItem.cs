using System;

namespace ExpenseManager.Application.Expenses.Models.Shared;

public sealed record ExpenseAttachmentItem(
    Guid Id,
    Guid ExpenseId,
    string ExpenseTitle,
    string FileName,
    string FilePath,
    long FileSizeInBytes,
    DateTime UploadedAt);
