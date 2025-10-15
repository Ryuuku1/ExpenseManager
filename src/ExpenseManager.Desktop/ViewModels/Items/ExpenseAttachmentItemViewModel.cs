using System;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class ExpenseAttachmentItemViewModel
{
    public ExpenseAttachmentItemViewModel(Guid id, string fileName, string filePath, long fileSizeInBytes, DateTime uploadedAt)
    {
        Id = id;
        FileName = fileName;
        FilePath = filePath;
        FileSizeInBytes = fileSizeInBytes;
        UploadedAt = uploadedAt;
    }

    public Guid Id { get; }
    public string FileName { get; }
    public string FilePath { get; }
    public long FileSizeInBytes { get; }
    public DateTime UploadedAt { get; }

    public string SizeDisplay => FileSizeInBytes switch
    {
        >= 1_000_000_000 => $"{FileSizeInBytes / 1_000_000_000d:0.##} GB",
        >= 1_000_000 => $"{FileSizeInBytes / 1_000_000d:0.##} MB",
        >= 1_000 => $"{FileSizeInBytes / 1_000d:0.##} KB",
        _ => $"{FileSizeInBytes} B"
    };

    public string UploadedAtDisplay => UploadedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
