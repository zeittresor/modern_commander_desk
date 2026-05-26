using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed class ImagePreviewHandler : IPreviewHandler
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".ico", ".tif", ".tiff"
    };

    public string Name => "Built-in image preview";
    public int Priority => 20;

    public bool CanPreview(string path)
    {
        return File.Exists(path) && Extensions.Contains(Path.GetExtension(path));
    }

    public Task<PreviewResult> CreatePreviewAsync(string path)
    {
        using var stream = File.OpenRead(path);
        var bitmap = new Bitmap(stream);
        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };
        var scroll = new ScrollViewer { Content = image };
        return Task.FromResult(new PreviewResult("Image preview - " + Path.GetFileName(path), scroll));
    }
}
