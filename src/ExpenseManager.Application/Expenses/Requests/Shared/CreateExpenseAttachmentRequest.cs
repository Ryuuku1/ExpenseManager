using System;

namespace ExpenseManager.Application.Expenses.Requests.Shared;

public sealed record CreateExpenseAttachmentRequest(
    string FileName,
    string FilePath,
    long FileSizeInBytes);
