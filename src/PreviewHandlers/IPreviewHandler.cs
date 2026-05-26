using Avalonia.Controls;

namespace ModernCommanderDesk.PreviewHandlers;

public sealed record PreviewResult(string Title, Control View);

public interface IPreviewHandler
{
    string Name { get; }
    int Priority { get; }
    bool CanPreview(string path);
    Task<PreviewResult> CreatePreviewAsync(string path);
}
