using System;

namespace ExpenseManager.Desktop.Services;

internal sealed class AppDataDirectoryProvider : IAppDataDirectoryProvider
{
    public string GetAppDataRoot()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }
}
