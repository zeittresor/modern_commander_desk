using Avalonia.Controls;
using Avalonia.Media;
using ModernCommanderDesk.Services;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed class TextPreviewHandler : IPreviewHandler
{
    private const int MaxPreviewBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".xml", ".html", ".htm", ".css", ".js", ".ts", ".cs", ".py", ".bat", ".cmd", ".ps1",
        ".sh", ".ini", ".cfg", ".conf", ".log", ".csv", ".yml", ".yaml", ".sql", ".c", ".cpp", ".h", ".hpp",
        ".java", ".rs", ".go", ".php", ".rb", ".lua", ".nfo", ".diz"
    };

    public string Name => "Built-in text preview";
    public int Priority => 10;

    public bool CanPreview(string path)
    {
        if (!File.Exists(path)) return false;
        var ext = Path.GetExtension(path);
        return Extensions.Contains(ext) || FileManager.IsTextPreviewCandidate(path);
    }

    public async Task<PreviewResult> CreatePreviewAsync(string path)
    {
        var file = new FileInfo(path);
        string text;
        if (file.Length > MaxPreviewBytes)
        {
            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var buffer = new char[MaxPreviewBytes];
            var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            text = new string(buffer, 0, read) + Environment.NewLine + Environment.NewLine + "[Preview truncated after 2 MB]";
        }
        else
        {
            text = await File.ReadAllTextAsync(path);
        }

        var box = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas, Menlo, monospace"),
            Text = text
        };
        return new PreviewResult("Text preview - " + Path.GetFileName(path), box);
    }
}
