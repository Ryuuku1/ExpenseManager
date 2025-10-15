using System;

namespace ExpenseManager.Desktop.Services;

internal interface IAppDataDirectoryProvider
{
    string GetAppDataRoot();
}
