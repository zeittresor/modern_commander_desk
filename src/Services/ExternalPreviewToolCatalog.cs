using System.Diagnostics;
using System.Text.Json;

namespace ModernCommanderDesk.Services;

public sealed class ExternalPreviewToolDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string[] Extensions { get; set; } = [];
    public string Executable { get; set; } = string.Empty;
    public string Arguments { get; set; } = "\"{file}\"";
}

public static class ExternalPreviewToolCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyList<ExternalPreviewToolDefinition> LoadDefinitions()
    {
        var result = new List<ExternalPreviewToolDefinition>();
        try
        {
            AppPaths.EnsureApplicationFolders();
            foreach (var file in Directory.EnumerateFiles(AppPaths.PreviewHandlersDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var single = JsonSerializer.Deserialize<ExternalPreviewToolDefinition>(json, JsonOptions);
                    if (single is not null && IsUsable(single))
                    {
                        result.Add(single);
                        continue;
                    }

                    var many = JsonSerializer.Deserialize<List<ExternalPreviewToolDefinition>>(json, JsonOptions);
                    if (many is not null)
                    {
                        result.AddRange(many.Where(IsUsable));
                    }
                }
                catch
                {
                    // Ignore broken preview handler manifests so a single bad plugin cannot break startup.
                }
            }
        }
        catch
        {
            // Keep the preview system optional.
        }

        return result;
    }

    public static ExternalPreviewToolDefinition? FindBest(string kind, string extension, IReadOnlyList<ExternalPreviewToolDefinition> fallbackCandidates)
    {
        var all = LoadDefinitions().Concat(fallbackCandidates);
        foreach (var tool in all)
        {
            if (!string.Equals(tool.Kind, kind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (tool.Extensions.Length > 0 && !tool.Extensions.Any(ext => string.Equals(NormalizeExtension(ext), NormalizeExtension(extension), StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (FindExecutable(tool.Executable) is not null)
            {
                return tool;
            }
        }

        return null;
    }

    public static Process? StartTool(ExternalPreviewToolDefinition tool, string filePath)
    {
        var executable = FindExecutable(tool.Executable) ?? tool.Executable;
        var arguments = BuildArguments(tool.Arguments, filePath);
        return Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory
        });
    }

    public static string BuildArguments(string template, string filePath)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            template = "\"{file}\"";
        }

        return template.Replace("{file}", filePath.Replace("\"", "\\\""));
    }

    public static string? FindExecutable(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            return null;
        }

        if (Path.IsPathRooted(executable))
        {
            if (File.Exists(executable))
            {
                return executable;
            }

            if (OperatingSystem.IsWindows() && string.IsNullOrWhiteSpace(Path.GetExtension(executable)) && File.Exists(executable + ".exe"))
            {
                return executable + ".exe";
            }
        }

        if (OperatingSystem.IsWindows())
        {
            foreach (var candidate in EnumerateKnownWindowsExecutableLocations(executable))
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var extensions = OperatingSystem.IsWindows()
            ? new[] { string.Empty, ".exe", ".cmd", ".bat" }
            : new[] { string.Empty };

        foreach (var path in paths)
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(path, executable.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? executable : executable + ext);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateKnownWindowsExecutableLocations(string executable)
    {
        var exeName = Path.GetFileName(executable.Trim().Trim('"'));
        if (string.IsNullOrWhiteSpace(exeName))
        {
            yield break;
        }

        var baseName = Path.GetFileNameWithoutExtension(exeName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(Path.GetExtension(exeName)))
        {
            exeName += ".exe";
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        IEnumerable<string> Roots()
        {
            if (!string.IsNullOrWhiteSpace(programFiles)) yield return programFiles;
            if (!string.IsNullOrWhiteSpace(programFilesX86)) yield return programFilesX86;
            if (!string.IsNullOrWhiteSpace(localAppData)) yield return localAppData;
        }

        foreach (var root in Roots())
        {
            if (baseName == "vlc")
            {
                yield return Path.Combine(root, "VideoLAN", "VLC", "vlc.exe");
            }
            else if (baseName == "mpv")
            {
                yield return Path.Combine(root, "mpv", "mpv.exe");
                yield return Path.Combine(root, "MPV", "mpv.exe");
            }
            else if (baseName == "ffplay")
            {
                yield return Path.Combine(root, "ffmpeg", "bin", "ffplay.exe");
                yield return Path.Combine(root, "FFmpeg", "bin", "ffplay.exe");
            }
        }

        if (baseName == "ffplay")
        {
            yield return @"C:\ffmpeg\bin\ffplay.exe";
            yield return @"C:\FFmpeg\bin\ffplay.exe";
        }
    }

    private static bool IsUsable(ExternalPreviewToolDefinition tool)
    {
        return !string.IsNullOrWhiteSpace(tool.Kind)
            && !string.IsNullOrWhiteSpace(tool.Executable)
            && !string.IsNullOrWhiteSpace(tool.Name);
    }

    private static string NormalizeExtension(string value)
    {
        value = value.Trim();
        return value.StartsWith('.') ? value : "." + value;
    }
}
