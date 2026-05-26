using System.Text.Json;

namespace ModernCommanderDesk.Services;

public sealed class ToolTipCatalog
{
    private readonly Dictionary<string, string> _tips;
    private readonly Dictionary<string, string> _fallback;

    public bool Enabled { get; }

    private ToolTipCatalog(Dictionary<string, string> tips, Dictionary<string, string> fallback, bool enabled)
    {
        _tips = tips;
        _fallback = fallback;
        Enabled = enabled;
    }

    public static ToolTipCatalog Load(string languageCode, bool enabled)
    {
        AppPaths.EnsureApplicationFolders();
        var safeCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
        var fallback = LoadJson("en");
        var tips = safeCode == "en" ? fallback : LoadJson(safeCode);
        return new ToolTipCatalog(tips, fallback, enabled);
    }

    public string Tip(string key)
    {
        if (_tips.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (_fallback.TryGetValue(key, out var fallbackValue) && !string.IsNullOrWhiteSpace(fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    private static Dictionary<string, string> LoadJson(string languageCode)
    {
        try
        {
            var path = Path.Combine(AppPaths.TooltipsDirectory, languageCode + ".json");
            if (!File.Exists(path))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
