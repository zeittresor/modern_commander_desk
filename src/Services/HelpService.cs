namespace ModernCommanderDesk.Services;

public static class HelpService
{
    public static string LoadHelp(string languageCode, string topic)
    {
        var safeLanguage = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
        var safeTopic = string.IsNullOrWhiteSpace(topic) ? "keyboard" : topic.Trim().ToLowerInvariant();
        var localized = Path.Combine(AppPaths.HelpDirectory, safeLanguage, safeTopic + ".md");
        var fallback = Path.Combine(AppPaths.HelpDirectory, "en", safeTopic + ".md");

        if (File.Exists(localized))
        {
            return File.ReadAllText(localized);
        }

        if (File.Exists(fallback))
        {
            return File.ReadAllText(fallback);
        }

        return "No help file found for topic: " + topic;
    }
}
