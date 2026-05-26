using ModernCommanderDesk.Models;

namespace ModernCommanderDesk.Services;

public static class FileManager
{
    public static IReadOnlyList<FileEntry> ListDirectory(string path, string? filter = null)
    {
        var result = new List<FileEntry>();
        var directory = new DirectoryInfo(path);
        if (!directory.Exists)
        {
            return result;
        }

        var normalizedFilter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        try
        {
            foreach (var dir in directory.EnumerateDirectories())
            {
                if (!MatchesFilter(dir.Name, normalizedFilter))
                {
                    continue;
                }

                result.Add(new FileEntry
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    IsDirectory = true,
                    Modified = SafeModified(dir),
                    Extension = string.Empty
                });
            }
        }
        catch
        {
            // Ignore folders that are not accessible and continue with files if possible.
        }

        try
        {
            foreach (var file in directory.EnumerateFiles())
            {
                if (!MatchesFilter(file.Name, normalizedFilter))
                {
                    continue;
                }

                result.Add(new FileEntry
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    IsDirectory = false,
                    SizeBytes = SafeLength(file),
                    Modified = SafeModified(file),
                    Extension = file.Extension
                });
            }
        }
        catch
        {
            // Ignore inaccessible files.
        }

        return result
            .OrderByDescending(x => x.IsDirectory)
            .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<NavItem> BuildNavigationItems()
    {
        var items = new List<NavItem>();
        AddSpecial(items, "Home", Environment.SpecialFolder.UserProfile, "🏠");
        AddSpecial(items, "Desktop", Environment.SpecialFolder.DesktopDirectory, "🖥️");
        AddSpecial(items, "Documents", Environment.SpecialFolder.MyDocuments, "📄");
        AddKnownFolder(items, "Downloads", Path.Combine(GetHomePath(), "Downloads"), "⬇️");
        AddSpecial(items, "Pictures", Environment.SpecialFolder.MyPictures, "🖼️");
        AddSpecial(items, "Music", Environment.SpecialFolder.MyMusic, "🎵");
        AddSpecial(items, "Videos", Environment.SpecialFolder.MyVideos, "🎬");

        foreach (var drive in DriveInfo.GetDrives().OrderBy(d => d.Name))
        {
            try
            {
                if (!drive.IsReady && OperatingSystem.IsWindows())
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? drive.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    : $"{drive.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}  {drive.VolumeLabel}";

                if (!items.Any(x => PathEquals(x.Path, drive.RootDirectory.FullName)))
                {
                    items.Add(new NavItem { Label = label, Path = drive.RootDirectory.FullName, Icon = "💽" });
                }
            }
            catch
            {
                // Some drives may throw if not ready or not accessible.
            }
        }

        if (!OperatingSystem.IsWindows())
        {
            AddKnownFolder(items, "Root /", "/", "🌐");
            AddKnownFolder(items, "Mounts /mnt", "/mnt", "🔌");
            AddKnownFolder(items, "Media /media", "/media", "💽");
        }

        return items;
    }

    public static string GetHomePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
        {
            return home;
        }

        return OperatingSystem.IsWindows()
            ? Environment.GetEnvironmentVariable("USERPROFILE") ?? "C:\\"
            : Environment.GetEnvironmentVariable("HOME") ?? "/";
    }

    public static bool IsTextPreviewCandidate(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return ext is "txt" or "md" or "log" or "ini" or "cfg" or "json" or "xml" or "yml" or "yaml"
            or "cs" or "py" or "js" or "ts" or "html" or "css" or "sh" or "bat" or "ps1" or "sql" or "csv";
    }

    public static string ReadPreviewText(string path, int maxBytes = 16384)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var length = (int)Math.Min(maxBytes, stream.Length);
        var buffer = new byte[length];
        _ = stream.Read(buffer, 0, length);
        var text = System.Text.Encoding.UTF8.GetString(buffer);
        if (stream.Length > maxBytes)
        {
            text += "\n\n… preview truncated …";
        }
        return text.Replace("\0", "");
    }

    public static void CopyTo(string sourcePath, string targetDirectory)
    {
        if (Directory.Exists(sourcePath))
        {
            var target = GetNonCollidingPath(Path.Combine(targetDirectory, Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))));
            CopyDirectory(sourcePath, target);
        }
        else if (File.Exists(sourcePath))
        {
            var target = GetNonCollidingPath(Path.Combine(targetDirectory, Path.GetFileName(sourcePath)));
            File.Copy(sourcePath, target);
        }
    }

    public static void MoveTo(string sourcePath, string targetDirectory)
    {
        var target = GetNonCollidingPath(Path.Combine(targetDirectory, Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))));
        if (Directory.Exists(sourcePath))
        {
            try
            {
                Directory.Move(sourcePath, target);
            }
            catch (IOException)
            {
                CopyDirectory(sourcePath, target);
                Directory.Delete(sourcePath, recursive: true);
            }
        }
        else if (File.Exists(sourcePath))
        {
            File.Move(sourcePath, target);
        }
    }

    public static string SoftDelete(string sourcePath)
    {
        var trashRoot = Path.Combine(GetHomePath(), ".ModernCommanderDeskTrash");
        Directory.CreateDirectory(trashRoot);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var name = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var target = GetNonCollidingPath(Path.Combine(trashRoot, $"{stamp}_{name}"));

        if (Directory.Exists(sourcePath))
        {
            try
            {
                Directory.Move(sourcePath, target);
            }
            catch (IOException)
            {
                CopyDirectory(sourcePath, target);
                Directory.Delete(sourcePath, recursive: true);
            }
        }
        else if (File.Exists(sourcePath))
        {
            File.Move(sourcePath, target);
        }

        return target;
    }

    public static void Rename(string sourcePath, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("The new name is empty.", nameof(newName));
        }

        var parent = Path.GetDirectoryName(sourcePath) ?? throw new InvalidOperationException("Could not determine parent directory.");
        var target = Path.Combine(parent, newName.Trim());
        if (PathExists(target))
        {
            throw new IOException("A file or folder with that name already exists.");
        }

        if (Directory.Exists(sourcePath))
        {
            Directory.Move(sourcePath, target);
        }
        else if (File.Exists(sourcePath))
        {
            File.Move(sourcePath, target);
        }
    }

    public static string CreateNewFolder(string currentPath)
    {
        var baseName = "New Folder";
        var candidate = Path.Combine(currentPath, baseName);
        var index = 2;
        while (Directory.Exists(candidate))
        {
            candidate = Path.Combine(currentPath, $"{baseName} {index}");
            index++;
        }

        Directory.CreateDirectory(candidate);
        return candidate;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var target = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, target);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var target = Path.Combine(destinationDir, Path.GetFileName(directory));
            CopyDirectory(directory, target);
        }
    }

    private static string GetNonCollidingPath(string path)
    {
        if (!PathExists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var index = 2;
        string candidate;
        do
        {
            candidate = Path.Combine(directory, $"{name} ({index}){extension}");
            index++;
        } while (PathExists(candidate));

        return candidate;
    }

    private static bool PathExists(string path) => File.Exists(path) || Directory.Exists(path);

    private static bool MatchesFilter(string name, string? filter)
        => filter is null || name.Contains(filter, StringComparison.CurrentCultureIgnoreCase);

    private static long SafeLength(FileInfo file)
    {
        try { return file.Length; }
        catch { return 0; }
    }

    private static DateTime SafeModified(FileSystemInfo info)
    {
        try { return info.LastWriteTime; }
        catch { return DateTime.MinValue; }
    }

    private static void AddSpecial(List<NavItem> items, string label, Environment.SpecialFolder folder, string icon)
    {
        var path = Environment.GetFolderPath(folder);
        AddKnownFolder(items, label, path, icon);
    }

    private static void AddKnownFolder(List<NavItem> items, string label, string path, string icon)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        if (!items.Any(x => PathEquals(x.Path, path)))
        {
            items.Add(new NavItem { Label = label, Path = path, Icon = icon });
        }
    }

    private static bool PathEquals(string a, string b)
        => string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                         Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                         OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
