using Avalonia.Controls;
using Avalonia.Media;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed class ModInfoPreviewHandler : IPreviewHandler
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mod", ".xm", ".s3m", ".it", ".med", ".okt", ".stm", ".669"
    };

    public string Name => "Built-in tracker module information preview";
    public int Priority => 30;

    public bool CanPreview(string path)
    {
        return File.Exists(path) && Extensions.Contains(Path.GetExtension(path));
    }

    public async Task<PreviewResult> CreatePreviewAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        var title = TryReadAscii(bytes, 0, Math.Min(20, bytes.Length)).Trim('\0', ' ');
        var signature = bytes.Length >= 1084 ? TryReadAscii(bytes, 1080, 4) : "";
        var info = new FileInfo(path);

        var panel = new StackPanel
        {
            Spacing = 10,
            Margin = new Avalonia.Thickness(10)
        };
        panel.Children.Add(new TextBlock
        {
            Text = "Tracker module preview",
            FontSize = 20,
            FontWeight = FontWeight.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Recognized as an Amiga / tracker-style module. v0.3 reads basic metadata and is prepared for an external playback handler in preview_handlers/.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock { Text = "File: " + info.Name });
        panel.Children.Add(new TextBlock { Text = "Title: " + (string.IsNullOrWhiteSpace(title) ? "unknown" : title) });
        panel.Children.Add(new TextBlock { Text = "Signature: " + (string.IsNullOrWhiteSpace(signature) ? "unknown" : signature) });
        panel.Children.Add(new TextBlock { Text = "Size: " + bytes.Length + " bytes" });
        return new PreviewResult("Module preview - " + info.Name, panel);
    }

    private static string TryReadAscii(byte[] bytes, int offset, int length)
    {
        if (offset < 0 || offset >= bytes.Length) return "";
        length = Math.Min(length, bytes.Length - offset);
        var chars = bytes.Skip(offset).Take(length).Select(b => b >= 32 && b <= 126 ? (char)b : ' ').ToArray();
        return new string(chars);
    }
}
