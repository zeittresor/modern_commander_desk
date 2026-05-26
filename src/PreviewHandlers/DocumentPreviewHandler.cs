using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ModernCommanderDesk.Services;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed class DocumentPreviewHandler : IPreviewHandler
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".rtf", ".pdf", ".docx", ".odt", ".epub"
    };

    public string Name => "Built-in document preview";
    public int Priority => 18;

    public bool CanPreview(string path) => File.Exists(path) && Extensions.Contains(Path.GetExtension(path));

    public async Task<PreviewResult> CreatePreviewAsync(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".rtf" => await CreateTextualDocumentPreviewAsync(path, "RTF document", ReadRtfAsPlainTextAsync),
            ".docx" => await CreateTextualDocumentPreviewAsync(path, "DOCX document", ReadDocxTextAsync),
            ".odt" => await CreateTextualDocumentPreviewAsync(path, "ODT document", ReadOdtTextAsync),
            ".pdf" => await CreatePdfInfoPreviewAsync(path),
            ".epub" => await CreateEpubInfoPreviewAsync(path),
            _ => await CreatePdfInfoPreviewAsync(path)
        };
    }

    private static async Task<PreviewResult> CreateTextualDocumentPreviewAsync(string path, string type, Func<string, Task<string>> reader)
    {
        string text;
        try
        {
            text = await reader(path);
        }
        catch (Exception ex)
        {
            text = "Could not extract text from this document." + Environment.NewLine + ex.Message;
        }

        var box = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas, Menlo, monospace"),
            Text = text
        };
        return new PreviewResult(type + " - " + Path.GetFileName(path), box);
    }

    private static Task<PreviewResult> CreatePdfInfoPreviewAsync(string path)
    {
        var info = new FileInfo(path);
        var text = new StringBuilder();
        text.AppendLine("PDF document");
        text.AppendLine();
        text.AppendLine("Embedded PDF rendering is intentionally left to an external preview helper/plugin because a reliable cross-platform PDF renderer adds sizeable native dependencies.");
        text.AppendLine();
        text.AppendLine("Use the button below to open the document with your default PDF viewer, or add a PDF helper manifest in preview_handlers/.");
        text.AppendLine();
        text.AppendLine("File: " + info.Name);
        text.AppendLine("Size: " + FormatBytes(info.Length));
        text.AppendLine("Modified: " + info.LastWriteTime);

        return Task.FromResult(BuildInfoPreview("PDF preview - " + info.Name, text.ToString(), path));
    }

    private static Task<PreviewResult> CreateEpubInfoPreviewAsync(string path)
    {
        var info = new FileInfo(path);
        var text = new StringBuilder();
        text.AppendLine("EPUB document");
        text.AppendLine();
        text.AppendLine("EPUB is recognized. Full EPUB rendering can be added through a preview helper/plugin.");
        text.AppendLine();
        text.AppendLine("File: " + info.Name);
        text.AppendLine("Size: " + FormatBytes(info.Length));
        text.AppendLine("Modified: " + info.LastWriteTime);

        return Task.FromResult(BuildInfoPreview("EPUB preview - " + info.Name, text.ToString(), path));
    }

    private static PreviewResult BuildInfoPreview(string title, string text, string path)
    {
        var box = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Text = text
        };

        var buttons = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };

        var open = new Button { Content = "Open externally", Width = 150, Margin = new Avalonia.Thickness(0, 0, 8, 6) };
        open.Click += (_, _) => PlatformOpen.OpenPath(path);
        buttons.Children.Add(open);

        var tool = ExternalPreviewToolCatalog.FindBest("document", Path.GetExtension(path), Array.Empty<ExternalPreviewToolDefinition>());
        if (tool is not null)
        {
            var helper = new Button { Content = "Open with " + tool.Name, Width = 180, Margin = new Avalonia.Thickness(0, 0, 8, 6) };
            helper.Click += (_, _) => ExternalPreviewToolCatalog.StartTool(tool, path);
            buttons.Children.Add(helper);
        }

        var panel = new StackPanel { Margin = new Avalonia.Thickness(10), Spacing = 8 };
        panel.Children.Add(box);
        panel.Children.Add(buttons);
        return new PreviewResult(title, panel);
    }

    private static async Task<string> ReadRtfAsPlainTextAsync(string path)
    {
        var rtf = await File.ReadAllTextAsync(path);
        var text = Regex.Replace(rtf, @"\\'[0-9a-fA-F]{2}", " ");
        text = Regex.Replace(text, @"\\[a-zA-Z]+-?\d* ?", " ");
        text = text.Replace("{", " ").Replace("}", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(text) ? "RTF document recognized, but no readable plain text could be extracted." : text;
    }

    private static Task<string> ReadDocxTextAsync(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null)
        {
            return Task.FromResult("DOCX document recognized, but word/document.xml was not found.");
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xml = reader.ReadToEnd();
        return Task.FromResult(XmlToPlainText(xml));
    }

    private static Task<string> ReadOdtTextAsync(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("content.xml");
        if (entry is null)
        {
            return Task.FromResult("ODT document recognized, but content.xml was not found.");
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xml = reader.ReadToEnd();
        return Task.FromResult(XmlToPlainText(xml));
    }

    private static string XmlToPlainText(string xml)
    {
        var withBreaks = Regex.Replace(xml, @"</(w:p|text:p|text:h|p)>", Environment.NewLine, RegexOptions.IgnoreCase);
        var withoutTags = Regex.Replace(withBreaks, "<[^>]+>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        decoded = Regex.Replace(decoded, @"[ \t]+", " ");
        decoded = Regex.Replace(decoded, @"(\r?\n\s*){3,}", Environment.NewLine + Environment.NewLine);
        return string.IsNullOrWhiteSpace(decoded) ? "Document recognized, but no readable plain text could be extracted." : decoded.Trim();
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
