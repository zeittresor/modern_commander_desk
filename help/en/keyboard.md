Modern Commander Desk v0.4.5 - Keyboard help

Tab: switch active panel
Enter: open file/folder
Backspace: go up
Ctrl+R: refresh active panel
F1: show this help
F2: rename selected item
F3: preview selected file through the preview handler registry
F4: edit selected text/code file
F5: copy active selection to passive panel
F6: move active selection to passive panel
F7: create folder in active panel
F8/Delete: soft-delete to ~/.ModernCommanderDeskTrash
F10: quit

The app uses separate resource folders:
- lang/: interface labels
- tooltips/: tooltip text
- help/: help documents
- plugins/: plugin manifests
- preview_handlers/: external preview/player handler notes and future drop-ins

Mouse:
- Drag a selected file/folder from one panel to the other.
- On drop, choose Copy, Move or Cancel.

Preview:
- F3 opens the preview handler registry.
- Images support zoom, slideshow and fullscreen.
- Audio/video use external helpers such as mpv, VLC or FFplay when available.
