using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ExpenseManager.Desktop.Services;

public sealed class FilePickerService : IFilePickerService
{
    public IReadOnlyCollection<PickedFile> PickFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            CheckFileExists = true,
            CheckPathExists = true
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return [];
        }

        var files = new List<PickedFile>(dialog.FileNames.Length);
        foreach (var filePath in dialog.FileNames)
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            var fileInfo = new FileInfo(filePath);
            files.Add(new PickedFile(fileInfo.Name, fileInfo.FullName, fileInfo.Length));
        }

        return files;
    }
}
