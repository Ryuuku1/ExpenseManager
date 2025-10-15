using System;

namespace ExpenseManager.Application.Expenses.Requests.Shared;

public sealed record CreateExpenseReceiptRequest(
    string FileName,
    string FilePath,
    long FileSizeInBytes);
