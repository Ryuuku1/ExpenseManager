using System;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class ReceiptItemViewModel
{
    public ReceiptItemViewModel(Guid id, Guid expenseId, string expenseTitle, string fileName, string filePath, long fileSizeInBytes, DateTime uploadedAt)
    {
        Id = id;
        ExpenseId = expenseId;
        ExpenseTitle = expenseTitle;
        FileName = fileName;
        FilePath = filePath;
        FileSizeInBytes = fileSizeInBytes;
        UploadedAt = uploadedAt;
    }

    public Guid Id { get; }
    public Guid ExpenseId { get; }
    public string ExpenseTitle { get; }
    public string FileName { get; }
    public string FilePath { get; }
    public long FileSizeInBytes { get; }
    public DateTime UploadedAt { get; }
}
