namespace ModernCommanderDesk.Services;

public static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string AppDataDirectory => Path.Combine(BaseDirectory, "app_data");
    public static string SettingsFile => Path.Combine(AppDataDirectory, "settings.json");
    public static string LanguageDirectory => Path.Combine(BaseDirectory, "lang");
    public static string TooltipsDirectory => Path.Combine(BaseDirectory, "tooltips");
    public static string HelpDirectory => Path.Combine(BaseDirectory, "help");
    public static string PluginsDirectory => Path.Combine(BaseDirectory, "plugins");
    public static string PreviewHandlersDirectory => Path.Combine(BaseDirectory, "preview_handlers");
    public static string HelpersDirectory => Path.Combine(BaseDirectory, "helpers");

    public static void EnsureApplicationFolders()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(LanguageDirectory);
        Directory.CreateDirectory(TooltipsDirectory);
        Directory.CreateDirectory(HelpDirectory);
        Directory.CreateDirectory(PluginsDirectory);
        Directory.CreateDirectory(PreviewHandlersDirectory);
        Directory.CreateDirectory(HelpersDirectory);
    }
}
