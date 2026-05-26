namespace ModernCommanderDesk.Models;

public sealed class FileEntry
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public bool IsDirectory { get; init; }
    public long SizeBytes { get; init; }
    public DateTime Modified { get; init; }
    public string Extension { get; init; } = string.Empty;
    public string Icon => IsDirectory ? "📁" : FileIconForExtension(Extension);
    public string TypeText => IsDirectory ? "Folder" : string.IsNullOrWhiteSpace(Extension) ? "File" : Extension.TrimStart('.').ToUpperInvariant() + " file";
    public string SizeText => IsDirectory ? "" : FormatSize(SizeBytes);
    public string ModifiedText => Modified.ToString("yyyy-MM-dd HH:mm");

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes} B" : $"{value:0.##} {units[unit]}";
    }

    private static string FileIconForExtension(string extension)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "png" or "jpg" or "jpeg" or "gif" or "bmp" or "webp" or "svg" => "🖼️",
            "mp3" or "wav" or "flac" or "ogg" or "m4a" => "🎵",
            "mp4" or "mkv" or "avi" or "mov" or "webm" => "🎬",
            "zip" or "7z" or "rar" or "tar" or "gz" => "🗜️",
            "txt" or "md" or "log" or "ini" => "📄",
            "cs" or "py" or "js" or "ts" or "html" or "css" or "json" or "xml" or "yml" or "yaml" or "sh" or "bat" => "🧩",
            "exe" or "msi" or "appimage" => "⚙️",
            "pdf" => "📕",
            _ => "📄"
        };
    }
}
