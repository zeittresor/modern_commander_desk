using Avalonia.Controls;
using Avalonia.Media;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed class HexPreviewHandler : IPreviewHandler
{
    private const int MaxBytes = 8192;

    public string Name => "Built-in hex preview";
    public int Priority => 999;

    public bool CanPreview(string path) => File.Exists(path);

    public async Task<PreviewResult> CreatePreviewAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var length = (int)Math.Min(stream.Length, MaxBytes);
        var buffer = new byte[length];
        var read = await stream.ReadAsync(buffer, 0, length);
        var text = BuildHexDump(buffer, read, stream.Length > MaxBytes);
        var box = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas, Menlo, monospace"),
            Text = text
        };
        return new PreviewResult("Hex preview - " + Path.GetFileName(path), box);
    }

    private static string BuildHexDump(byte[] buffer, int length, bool truncated)
    {
        var lines = new List<string>();
        for (var i = 0; i < length; i += 16)
        {
            var chunk = buffer.Skip(i).Take(Math.Min(16, length - i)).ToArray();
            var hex = string.Join(" ", chunk.Select(b => b.ToString("X2"))).PadRight(16 * 3);
            var ascii = new string(chunk.Select(b => b >= 32 && b <= 126 ? (char)b : '.').ToArray());
            lines.Add($"{i:X8}  {hex}  {ascii}");
        }

        if (truncated)
        {
            lines.Add("");
            lines.Add("[Hex preview truncated after 8192 bytes]");
        }
        return string.Join(Environment.NewLine, lines);
    }
}
