using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed class ImagePreviewHandler : IPreviewHandler
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".ico", ".tif", ".tiff"
    };

    public string Name => "Built-in image viewer";
    public int Priority => 20;

    public bool CanPreview(string path)
    {
        return File.Exists(path) && Extensions.Contains(Path.GetExtension(path));
    }

    public Task<PreviewResult> CreatePreviewAsync(string path)
    {
        var files = GetSiblingImages(path);
        var index = Math.Max(0, files.FindIndex(x => string.Equals(x, path, CurrentPathComparison())));
        var zoom = 1.0;
        var fitToWindow = true;

        var image = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        var info = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var zoomLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Width = 70 };
        var scroll = new ScrollViewer { Content = image };

        void LoadCurrent()
        {
            if (files.Count == 0) return;
            var current = files[index];
            image.Source = LoadBitmap(current);
            ApplyZoom();
            var file = new FileInfo(current);
            info.Text = $"{index + 1}/{files.Count}  •  {file.Name}  •  {FormatBytes(file.Length)}";
        }

        void ApplyZoom()
        {
            if (image.Source is not Bitmap bitmap)
            {
                return;
            }

            if (fitToWindow)
            {
                image.Width = double.NaN;
                image.Height = double.NaN;
                image.Stretch = Stretch.Uniform;
                image.HorizontalAlignment = HorizontalAlignment.Stretch;
                image.VerticalAlignment = VerticalAlignment.Stretch;
                zoomLabel.Text = "Fit";
                return;
            }

            image.Stretch = Stretch.Fill;
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Width = Math.Max(1, bitmap.PixelSize.Width * zoom);
            image.Height = Math.Max(1, bitmap.PixelSize.Height * zoom);
            zoomLabel.Text = $"{zoom * 100:0}%";
        }

        void Move(int delta)
        {
            if (files.Count == 0) return;
            index = (index + delta + files.Count) % files.Count;
            LoadCurrent();
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) => Move(1);

        var previous = new Button { Content = "◀ Prev", Width = 90 };
        var next = new Button { Content = "Next ▶", Width = 90 };
        var zoomOut = new Button { Content = "−", Width = 42 };
        var zoomIn = new Button { Content = "+", Width = 42 };
        var fit = new Button { Content = "Fit", Width = 72 };
        var actual = new Button { Content = "100%", Width = 72 };
        var slide = new Button { Content = "Slideshow", Width = 105 };
        var full = new Button { Content = "Fullscreen", Width = 110 };

        previous.Click += (_, _) => Move(-1);
        next.Click += (_, _) => Move(1);
        zoomOut.Click += (_, _) => { fitToWindow = false; zoom = Math.Max(0.1, zoom / 1.25); ApplyZoom(); };
        zoomIn.Click += (_, _) => { fitToWindow = false; zoom = Math.Min(8.0, zoom * 1.25); ApplyZoom(); };
        fit.Click += (_, _) => { fitToWindow = true; ApplyZoom(); };
        actual.Click += (_, _) => { fitToWindow = false; zoom = 1.0; ApplyZoom(); };
        slide.Click += (_, _) =>
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                slide.Content = "Slideshow";
            }
            else
            {
                timer.Start();
                slide.Content = "Stop show";
            }
        };
        full.Click += (_, _) => ShowFullscreen(files[index]);

        var toolbar = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Avalonia.Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        foreach (var control in new Control[] { previous, next, zoomOut, zoomLabel, zoomIn, fit, actual, slide, full })
        {
            control.Margin = new Avalonia.Thickness(0, 0, 8, 6);
            toolbar.Children.Add(control);
        }

        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto")
        };
        Grid.SetRow(toolbar, 0);
        Grid.SetRow(scroll, 1);
        Grid.SetRow(info, 2);
        root.Children.Add(toolbar);
        root.Children.Add(scroll);
        root.Children.Add(info);
        root.DetachedFromVisualTree += (_, _) => timer.Stop();

        LoadCurrent();
        return Task.FromResult(new PreviewResult("Image viewer - " + Path.GetFileName(path), root));
    }

    private static Bitmap LoadBitmap(string path)
    {
        using var stream = File.OpenRead(path);
        return new Bitmap(stream);
    }

    private static List<string> GetSiblingImages(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return [path];
        }

        try
        {
            return Directory.EnumerateFiles(directory)
                .Where(file => Extensions.Contains(Path.GetExtension(file)))
                .OrderBy(file => Path.GetFileName(file), StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch
        {
            return [path];
        }
    }

    private static void ShowFullscreen(string path)
    {
        var image = new Image
        {
            Source = LoadBitmap(path),
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        var window = new Window
        {
            Title = "Fullscreen - " + Path.GetFileName(path),
            WindowState = WindowState.FullScreen,
            Background = Brushes.Black,
            Content = image
        };
        window.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape || e.Key == Key.F11)
            {
                window.Close();
            }
        };
        window.Show();
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

    private static StringComparison CurrentPathComparison()
        => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
