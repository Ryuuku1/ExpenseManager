using System;
using System.IO;

namespace ExpenseManager.Scripts.RemoveExpenseManagerLocalData;

internal static class Program
{
    private const string FolderName = "ExpenseManager";

    public static int Main(string[] args)
    {
        var force = false;
        var noPause = false;
        var sawInvalidOption = false;

        foreach (var arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--force":
                case "-f":
                    force = true;
                    break;
                case "--no-pause":
                    noPause = true;
                    break;
                case "--help":
                case "-h":
                case "/?":
                    ShowUsage();
                    PauseIfNeeded(noPause);
                    return 0;
                default:
                    Console.Error.WriteLine($"Unrecognized option '{arg}'.");
                    sawInvalidOption = true;
                    break;
            }
        }

        if (sawInvalidOption)
        {
            ShowUsage();
            PauseIfNeeded(noPause);
            return 1;
        }

        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            Console.Error.WriteLine("LOCALAPPDATA environment variable is not set.");
            PauseIfNeeded(noPause);
            return 1;
        }

        var targetPath = Path.Combine(localAppData, FolderName);
        if (!Directory.Exists(targetPath))
        {
            Console.WriteLine($"Nothing to delete. Folder '{targetPath}' does not exist.");
            PauseIfNeeded(noPause);
            return 0;
        }

        if (!force)
        {
            Console.Write($"Delete '{targetPath}'? Type YES to confirm: ");
            var response = Console.ReadLine();
            if (!string.Equals(response, "YES", StringComparison.Ordinal))
            {
                Console.WriteLine("Deletion aborted.");
                PauseIfNeeded(noPause);
                return 1;
            }
        }

        try
        {
            Directory.Delete(targetPath, recursive: true);
            Console.WriteLine($"Deleted '{targetPath}'.");
            PauseIfNeeded(noPause);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to delete '{targetPath}'. {ex.Message}");
            PauseIfNeeded(noPause);
            return 1;
        }
    }

    private static void PauseIfNeeded(bool noPause)
    {
        if (noPause || !ShouldPause()) return;

        Console.WriteLine();
        Console.Write("Press any key to exit...");
        Console.ReadKey(intercept: true);
    }

    private static bool ShouldPause()
    {
        if (!Environment.UserInteractive) return false;
        if (Console.IsInputRedirected) return false;

        try
        {
            return Console.WindowHeight > 0;
        }
        catch
        {
            return false;
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine(
            """
            Usage: RemoveExpenseManagerLocalData [options]

            Options:
              --force, -f      Skips the confirmation prompt.
              --no-pause       Does not wait for a keypress before closing.
              --help,  -h      Shows this help.
            """);
    }
}
