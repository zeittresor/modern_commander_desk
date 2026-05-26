namespace ModernCommanderDesk.Services;

public sealed class AppSettings
{
    public string Language { get; set; } = "en";
    public bool TooltipsEnabled { get; set; } = true;
    public string Theme { get; set; } = "dark";
}
