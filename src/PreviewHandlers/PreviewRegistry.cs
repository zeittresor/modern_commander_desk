namespace ModernCommanderDesk.PreviewHandlers;

public static class PreviewRegistry
{
    private static readonly IReadOnlyList<IPreviewHandler> BuiltInHandlers = new IPreviewHandler[]
    {
        new TextPreviewHandler(),
        new DocumentPreviewHandler(),
        new ImagePreviewHandler(),
        new AudioPlayerPreviewHandler(),
        new VideoPlayerPreviewHandler(),
        new ModInfoPreviewHandler(),
        new MediaInfoPreviewHandler(),
        new HexPreviewHandler()
    }.OrderBy(x => x.Priority).ToList();

    public static IPreviewHandler GetHandler(string path)
    {
        return BuiltInHandlers.FirstOrDefault(x => x.CanPreview(path)) ?? BuiltInHandlers.Last();
    }

    public static Task<PreviewResult> CreatePreviewAsync(string path)
    {
        return GetHandler(path).CreatePreviewAsync(path);
    }
}
