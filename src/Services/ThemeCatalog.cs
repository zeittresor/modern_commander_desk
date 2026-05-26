using Avalonia.Media;

namespace ModernCommanderDesk.Services;

public static class ThemeCatalog
{
    public static readonly string[] Codes = ["dark", "light", "matrix", "hell", "amiga_mui"];

    public static string Normalize(string? theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
        {
            return "dark";
        }

        var code = theme.Trim().ToLowerInvariant();
        return Codes.Contains(code) ? code : "dark";
    }

    public static bool UsesLightFluentVariant(string? theme)
    {
        var code = Normalize(theme);
        return code is "light" or "amiga_mui";
    }

    public static string DisplayName(string code, LocalizationService loc)
    {
        var normalized = Normalize(code);
        var fallback = normalized switch
        {
            "dark" => "Dark Commander",
            "light" => "Light Explorer",
            "matrix" => "Matrix Green",
            "hell" => "Hell Red",
            "amiga_mui" => "Amiga MUI",
            _ => normalized
        };
        return loc.T("theme." + normalized, fallback);
    }

    public static IBrush Brush(string? theme, string role)
        => new SolidColorBrush(Avalonia.Media.Color.Parse(GetColor(theme, role)));

    public static string GetColor(string? theme, string role)
    {
        var code = Normalize(theme);
        return code switch
        {
            "light" => Light(role),
            "matrix" => Matrix(role),
            "hell" => Hell(role),
            "amiga_mui" => AmigaMui(role),
            _ => Dark(role)
        };
    }

    private static string Dark(string role) => role switch
    {
        "window_bg" => "#0e1118",
        "menu_bg" => "#151a26",
        "toolbar_bg" => "#111722",
        "status_bg" => "#141925",
        "panel_bg" => "#11151d",
        "dialog_bg" => "#151923",
        "border" => "#293041",
        "splitter" => "#2a3140",
        "text" => "#f2f4f8",
        "muted" => "#8f9ab0",
        "status_text" => "#b6c2d6",
        "active" => "#6ea8ff",
        "active_text" => "#9ec5ff",
        "error" => "#ff8a8a",
        _ => "#f2f4f8"
    };

    private static string Light(string role) => role switch
    {
        "window_bg" => "#f3f5f8",
        "menu_bg" => "#e8edf5",
        "toolbar_bg" => "#eef2f8",
        "status_bg" => "#e8edf5",
        "panel_bg" => "#ffffff",
        "dialog_bg" => "#ffffff",
        "border" => "#c6cedb",
        "splitter" => "#c6cedb",
        "text" => "#111827",
        "muted" => "#4b5563",
        "status_text" => "#1f2937",
        "active" => "#3478f6",
        "active_text" => "#0f4cbd",
        "error" => "#b42318",
        _ => "#111827"
    };

    private static string Matrix(string role) => role switch
    {
        "window_bg" => "#020804",
        "menu_bg" => "#031307",
        "toolbar_bg" => "#041a09",
        "status_bg" => "#031307",
        "panel_bg" => "#06100a",
        "dialog_bg" => "#06100a",
        "border" => "#145c2a",
        "splitter" => "#0c3d1c",
        "text" => "#d7ffd7",
        "muted" => "#76b982",
        "status_text" => "#9dff9d",
        "active" => "#00ff66",
        "active_text" => "#66ff99",
        "error" => "#ff6b6b",
        _ => "#d7ffd7"
    };

    private static string Hell(string role) => role switch
    {
        "window_bg" => "#170607",
        "menu_bg" => "#25090a",
        "toolbar_bg" => "#2c0b0b",
        "status_bg" => "#25090a",
        "panel_bg" => "#1b0b0b",
        "dialog_bg" => "#230c0c",
        "border" => "#5c1b1b",
        "splitter" => "#421111",
        "text" => "#fff0dc",
        "muted" => "#d99a78",
        "status_text" => "#ffd2a6",
        "active" => "#ff5a2e",
        "active_text" => "#ffb087",
        "error" => "#ff8a8a",
        _ => "#fff0dc"
    };

    private static string AmigaMui(string role) => role switch
    {
        "window_bg" => "#aeb8c6",
        "menu_bg" => "#8d99aa",
        "toolbar_bg" => "#9da8b8",
        "status_bg" => "#8d99aa",
        "panel_bg" => "#c0c8d5",
        "dialog_bg" => "#c7ced9",
        "border" => "#4c5c74",
        "splitter" => "#6b7789",
        "text" => "#111827",
        "muted" => "#344054",
        "status_text" => "#111827",
        "active" => "#1f4fa3",
        "active_text" => "#0f2f6a",
        "error" => "#8a1c1c",
        _ => "#111827"
    };
}
