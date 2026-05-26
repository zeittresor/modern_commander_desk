namespace ModernCommanderDesk.Models;

public sealed class NavItem
{
    public required string Label { get; init; }
    public required string Path { get; init; }
    public string Icon { get; init; } = "📁";
    public override string ToString() => $"{Icon}  {Label}";
}
