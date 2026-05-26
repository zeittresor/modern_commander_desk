using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed class MediaInfoPreviewHandler : IPreviewHandler
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3", ".flac", ".ogg", ".m4a", ".aac", ".mp4", ".mkv", ".webm", ".avi", ".mov", ".wmv"
    };

    public string Name => "Built-in media information preview";
    public int Priority => 40;

    public bool CanPreview(string path)
    {
        return File.Exists(path) && Extensions.Contains(Path.GetExtension(path));
    }

    public Task<PreviewResult> CreatePreviewAsync(string path)
    {
        var info = new FileInfo(path);
        var panel = new StackPanel
        {
            Spacing = 10,
            Margin = new Avalonia.Thickness(10)
        };
        panel.Children.Add(new TextBlock
        {
            Text = "Media preview handler",
            FontSize = 20,
            FontWeight = FontWeight.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = "This v0.3 handler recognizes common audio/video files and routes them through the preview architecture. Embedded playback is intentionally left as a replaceable preview_handler module; use Open to play the file with the system player.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock { Text = "File: " + info.Name });
        panel.Children.Add(new TextBlock { Text = "Type: " + Path.GetExtension(path).TrimStart('.').ToUpperInvariant() });
        panel.Children.Add(new TextBlock { Text = "Size: " + FormatBytes(info.Length) });
        panel.Children.Add(new TextBlock { Text = "Modified: " + info.LastWriteTime });
        return Task.FromResult(new PreviewResult("Media info - " + info.Name, panel));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.##} {units[unit]}";
    }
}
