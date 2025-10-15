using System.Collections.Generic;

namespace ExpenseManager.Desktop.Services;

public interface IFilePickerService
{
    IReadOnlyCollection<PickedFile> PickFiles();
}

public sealed record PickedFile(string FileName, string FullPath, long FileSizeInBytes);
