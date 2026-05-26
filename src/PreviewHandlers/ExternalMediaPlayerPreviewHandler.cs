using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ModernCommanderDesk.Services;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed class AudioPlayerPreviewHandler : IPreviewHandler
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3", ".ogg", ".oga", ".flac", ".m4a", ".aac", ".wma", ".mid", ".midi",
        ".mod", ".xm", ".s3m", ".it", ".med", ".okt", ".stm", ".669"
    };

    public string Name => "External-helper audio player";
    public int Priority => 28;
    public bool CanPreview(string path) => File.Exists(path) && Extensions.Contains(Path.GetExtension(path));
    public Task<PreviewResult> CreatePreviewAsync(string path) => ExternalMediaPreviewFactory.CreateAsync(path, "audio", AudioFallbackTools());

    private static IReadOnlyList<ExternalPreviewToolDefinition> AudioFallbackTools() =>
    [
        new() { Id = "mpv-audio", Name = "mpv", Kind = "audio", Extensions = Extensions.ToArray(), Executable = "mpv", Arguments = "--force-window=yes \"{file}\"" },
        new() { Id = "vlc-audio", Name = "VLC", Kind = "audio", Extensions = Extensions.ToArray(), Executable = "vlc", Arguments = "\"{file}\"" },
        new() { Id = "ffplay-audio", Name = "FFplay", Kind = "audio", Extensions = Extensions.ToArray(), Executable = "ffplay", Arguments = "-autoexit -hide_banner \"{file}\"" },
        new() { Id = "openmpt123", Name = "openmpt123", Kind = "audio", Extensions = [".mod", ".xm", ".s3m", ".it", ".med", ".okt", ".stm", ".669"], Executable = "openmpt123", Arguments = "\"{file}\"" },
        new() { Id = "timidity", Name = "TiMidity++", Kind = "audio", Extensions = [".mid", ".midi"], Executable = "timidity", Arguments = "\"{file}\"" }
    ];
}

public sealed class VideoPlayerPreviewHandler : IPreviewHandler
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".m4v", ".mkv", ".webm", ".avi", ".mov", ".wmv", ".mpg", ".mpeg", ".ts", ".m2ts", ".ogv"
    };

    public string Name => "External-helper video player";
    public int Priority => 32;
    public bool CanPreview(string path) => File.Exists(path) && Extensions.Contains(Path.GetExtension(path));
    public Task<PreviewResult> CreatePreviewAsync(string path) => ExternalMediaPreviewFactory.CreateAsync(path, "video", VideoFallbackTools());

    private static IReadOnlyList<ExternalPreviewToolDefinition> VideoFallbackTools()
    {
        var vlc = new ExternalPreviewToolDefinition { Id = "vlc-video", Name = "VLC", Kind = "video", Extensions = Extensions.ToArray(), Executable = "vlc", Arguments = "\"{file}\"" };
        var mpv = new ExternalPreviewToolDefinition { Id = "mpv-video", Name = "mpv", Kind = "video", Extensions = Extensions.ToArray(), Executable = "mpv", Arguments = "\"{file}\"" };
        var ffplay = new ExternalPreviewToolDefinition { Id = "ffplay-video", Name = "FFplay", Kind = "video", Extensions = Extensions.ToArray(), Executable = "ffplay", Arguments = "-autoexit -hide_banner -loglevel warning \"{file}\"" };

        // On Windows, VLC is often installed but not added to PATH. v0.4.5 therefore
        // prefers VLC/mpv for video when they can be discovered in Program Files/PATH, because
        // FFplay/FFmpeg helper windows can appear briefly and then vanish on some Windows setups.
        // Users who explicitly want FFplay on Windows can add it as a custom JSON helper.
        return OperatingSystem.IsWindows()
            ? [vlc, mpv]
            : [mpv, vlc, ffplay];
    }

}

internal static class ExternalMediaPreviewFactory
{
    public static Task<PreviewResult> CreateAsync(string path, string kind, IReadOnlyList<ExternalPreviewToolDefinition> fallbackTools)
    {
        var info = new FileInfo(path);
        var ext = Path.GetExtension(path);
        var tool = ExternalPreviewToolCatalog.FindBest(kind, ext, fallbackTools);
        Process? runningProcess = null;

        var title = CultureTitle(kind) + " preview";
        var status = new TextBlock
        {
            Text = tool is null
                ? "No external player helper found. Install mpv, VLC or FFplay, or add a JSON helper manifest in preview_handlers/."
                : $"Ready to play with: {tool.Name}",
            TextWrapping = TextWrapping.Wrap
        };

        var play = new Button { Content = "▶ Play", Width = 100, IsEnabled = tool is not null };
        var stop = new Button { Content = "■ Stop", Width = 100, IsEnabled = false };
        var openExternal = new Button { Content = "Open default app", Width = 150 };
        var openPreviewFolder = new Button { Content = "Open preview_handlers", Width = 180 };

        play.Click += (_, _) =>
        {
            if (tool is null)
            {
                return;
            }

            try
            {
                if (runningProcess is not null && !runningProcess.HasExited)
                {
                    runningProcess.Kill(entireProcessTree: true);
                }

                runningProcess = ExternalPreviewToolCatalog.StartTool(tool, path);
                stop.IsEnabled = runningProcess is not null;
                status.Text = runningProcess is null ? "Player could not be started." : "Playing with: " + tool.Name;
            }
            catch (Exception ex)
            {
                status.Text = "Could not start player: " + ex.Message;
            }
        };

        stop.Click += (_, _) =>
        {
            try
            {
                if (runningProcess is not null && !runningProcess.HasExited)
                {
                    runningProcess.Kill(entireProcessTree: true);
                }
                status.Text = "Playback stopped.";
            }
            catch (Exception ex)
            {
                status.Text = "Could not stop player: " + ex.Message;
            }
            finally
            {
                stop.IsEnabled = false;
            }
        };

        openExternal.Click += (_, _) => PlatformOpen.OpenPath(path);
        openPreviewFolder.Click += (_, _) => PlatformOpen.OpenPath(AppPaths.PreviewHandlersDirectory);

        var buttons = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Avalonia.Thickness(0, 10, 0, 10)
        };
        foreach (var control in new Control[] { play, stop, openExternal, openPreviewFolder })
        {
            control.Margin = new Avalonia.Thickness(0, 0, 8, 6);
            buttons.Children.Add(control);
        }

        var panel = new StackPanel
        {
            Spacing = 10,
            Margin = new Avalonia.Thickness(10)
        };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 22,
            FontWeight = FontWeight.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = kind == "audio"
                ? "Audio playback is handled through replaceable external helper tools. This keeps the main file manager small and cross-platform. For tracker modules, openmpt123/mpv/VLC are useful helpers."
                : "Video playback is handled through replaceable external helper tools. v0.4.5 improves VLC discovery on Windows and falls back to mpv/FFplay if available. This keeps the main file manager small and avoids bundling heavy codec libraries.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(status);
        panel.Children.Add(buttons);
        panel.Children.Add(new TextBlock { Text = "File: " + info.Name });
        panel.Children.Add(new TextBlock { Text = "Type: " + ext.TrimStart('.').ToUpperInvariant() });
        panel.Children.Add(new TextBlock { Text = "Size: " + FormatBytes(info.Length) });
        panel.Children.Add(new TextBlock { Text = "Modified: " + info.LastWriteTime });
        if (tool is not null)
        {
            panel.Children.Add(new TextBlock { Text = "Helper: " + tool.Name + "  (" + tool.Executable + " " + tool.Arguments + ")", TextWrapping = TextWrapping.Wrap });
        }

        panel.DetachedFromVisualTree += (_, _) =>
        {
            try
            {
                if (runningProcess is not null && !runningProcess.HasExited)
                {
                    runningProcess.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore process shutdown errors on preview close.
            }
        };

        return Task.FromResult(new PreviewResult(title + " - " + info.Name, new ScrollViewer { Content = panel }));
    }

    private static string CultureTitle(string kind)
        => string.Equals(kind, "video", StringComparison.OrdinalIgnoreCase) ? "Video" : "Audio";

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
