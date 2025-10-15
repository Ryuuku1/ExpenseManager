using System;

namespace ExpenseManager.Desktop.ViewModels.Items;

public sealed class ExpenseAttachmentInputViewModel
{
    public ExpenseAttachmentInputViewModel(Guid? attachmentId, string fileName, string filePath, long fileSizeInBytes, bool isNew)
    {
        AttachmentId = attachmentId;
        FileName = fileName;
        FilePath = filePath;
        FileSizeInBytes = fileSizeInBytes;
        IsNew = isNew;
    }

    public Guid? AttachmentId { get; }
    public string FileName { get; }
    public string FilePath { get; }
    public long FileSizeInBytes { get; }
    public bool IsNew { get; }

    public string SizeDisplay => FileSizeInBytes switch
    {
        >= 1_000_000_000 => $"{FileSizeInBytes / 1_000_000_000d:0.##} GB",
        >= 1_000_000 => $"{FileSizeInBytes / 1_000_000d:0.##} MB",
        >= 1_000 => $"{FileSizeInBytes / 1_000d:0.##} KB",
        _ => $"{FileSizeInBytes} B"
    };
}
