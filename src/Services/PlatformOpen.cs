using System.Diagnostics;

namespace ModernCommanderDesk.Services;

public static class PlatformOpen
{
    public static void OpenPath(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", Quote(path));
            return;
        }

        Process.Start("xdg-open", Quote(path));
    }

    public static void OpenTerminal(string workingDirectory)
    {
        if (OperatingSystem.IsWindows())
        {
            if (TryStart("wt.exe", $"-d {Quote(workingDirectory)}"))
            {
                return;
            }

            TryStart("cmd.exe", $"/K cd /d {Quote(workingDirectory)}");
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            TryStart("open", $"-a Terminal {Quote(workingDirectory)}");
            return;
        }

        var candidates = new[]
        {
            ("x-terminal-emulator", $"--working-directory={Quote(workingDirectory)}"),
            ("gnome-terminal", $"--working-directory={Quote(workingDirectory)}"),
            ("konsole", $"--workdir {Quote(workingDirectory)}"),
            ("xfce4-terminal", $"--working-directory={Quote(workingDirectory)}"),
            ("xterm", $"-e sh -lc 'cd {EscapeForSingleQuotes(workingDirectory)}; exec sh'")
        };

        foreach (var (fileName, args) in candidates)
        {
            if (TryStart(fileName, args))
            {
                return;
            }
        }
    }

    private static bool TryStart(string fileName, string arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    private static string EscapeForSingleQuotes(string value) => value.Replace("'", "'\\''");
}
