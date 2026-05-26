# Modern Commander Desk v0.3.3

Hotfix and UI cleanup release.

## Changed

- Removed the toolbar `Theme` button because theme selection belongs in the Settings menu.
- Added `Settings -> Visual theme` submenu.
- Added selectable visual themes:
  - Dark Commander
  - Light Explorer
  - Matrix Green
  - Hell Red
  - Amiga MUI
- Theme selection now rebuilds the interface while keeping the current left/right panel paths.
- Theme colors are centralized in `src/Services/ThemeCatalog.cs` instead of being scattered through the UI logic.
- Updated About text/version to v0.3.3.

## Notes

The previous toolbar button only toggled Avalonia's dark/light variant while much of the Commander UI still used hard-coded dark colors. That produced the strange greyed-out/mixed look shown in testing. v0.3.3 moves the feature to Settings and applies a small central palette layer.
