# Modern Commander Desk v0.3.3

Hotfix release after v0.3.2 testing.

## Fixed

- Fixed CS0119 build error in `ThemeCatalog.cs` caused by a name collision between the `Color(...)` helper method and `Avalonia.Media.Color`.
- Renamed the helper to `GetColor(...)` and explicitly calls `Avalonia.Media.Color.Parse(...)`.
- Updated Windows/Linux build and publish script headers from v0.3.1 to v0.3.3.
- Updated application/about/help visible version strings to v0.3.3.

## Notes

The theme feature remains in `Settings -> Visual theme`; the top toolbar theme button remains removed.
