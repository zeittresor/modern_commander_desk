using System.Text.Json;

namespace ModernCommanderDesk.Services;

public sealed class LocalizationService
{
    private readonly Dictionary<string, string> _strings;
    private readonly Dictionary<string, string> _fallback;

    public string LanguageCode { get; }

    private LocalizationService(string languageCode, Dictionary<string, string> strings, Dictionary<string, string> fallback)
    {
        LanguageCode = languageCode;
        _strings = strings;
        _fallback = fallback;
    }

    public static LocalizationService Load(string languageCode)
    {
        AppPaths.EnsureApplicationFolders();
        var safeCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
        var fallback = LoadJson("en");
        var strings = safeCode == "en" ? fallback : LoadJson(safeCode);
        return new LocalizationService(safeCode, strings, fallback);
    }

    public string T(string key, string fallback)
    {
        if (_strings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (_fallback.TryGetValue(key, out var fallbackValue) && !string.IsNullOrWhiteSpace(fallbackValue))
        {
            return fallbackValue;
        }

        return fallback;
    }

    public string Format(string key, string fallback, params object[] args)
    {
        return string.Format(T(key, fallback), args);
    }

    public IReadOnlyList<string> GetAvailableLanguages()
    {
        if (!Directory.Exists(AppPaths.LanguageDirectory))
        {
            return new[] { "en" };
        }

        var files = Directory.GetFiles(AppPaths.LanguageDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .OrderBy(x => x)
            .ToList();

        if (!files.Contains("en", StringComparer.OrdinalIgnoreCase))
        {
            files.Insert(0, "en");
        }

        return files;
    }

    public string GetLanguageName(string code)
    {
        var key = "language." + code.ToLowerInvariant();
        return T(key, code.ToUpperInvariant());
    }

    private static Dictionary<string, string> LoadJson(string languageCode)
    {
        try
        {
            var path = Path.Combine(AppPaths.LanguageDirectory, languageCode + ".json");
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
