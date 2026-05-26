# Modern Commander Desk v0.3.3

A cross-platform, dual-pane commander-style file manager written in C# / Avalonia.

<img width="1599" height="887" alt="modern_commander_desk" src="https://github.com/user-attachments/assets/a31f8c5d-b4a5-4c11-b708-f23ede1f63df" />

## Main features

- Left/right dual-pane view
- Active/passive panel workflow
- Menu bar: File, Commands, Navigate, Tools, Settings, Help
- F-key bar: F1 Help, F2 Rename, F3 View, F4 Edit, F5 Copy, F6 Move, F7 MkDir, F8 Delete, F10 Quit
- Copy/move from active panel to passive panel
- Soft-delete to `~/.ModernCommanderDeskTrash`
- Terminal in active folder
- Settings > Visual theme with Dark Commander, Light Explorer, Matrix Green, Hell Red and Amiga MUI
- Language switching from Settings > Language
- Tooltips can be turned on/off from Settings
- Interface strings stored in `lang/*.json`
- Tooltip strings stored in `tooltips/*.json`
- Help files stored in `help/<language>/*.md`
- Plugin manifest folder: `plugins/`
- Preview-handler architecture in `src/PreviewHandlers/`

## Preview handlers in v0.3

Built-in preview routing currently supports:

- text/code preview
- image preview
- tracker module metadata preview for `.mod`, `.xm`, `.s3m`, `.it`, `.med`, `.okt`, etc.
- audio/video information placeholder for `.wav`, `.mp3`, `.flac`, `.ogg`, `.mp4`, `.mkv`, etc.
- hex fallback for unknown file types

Embedded MOD/MP3/video playback is intentionally not hardwired in v0.3. The folder `preview_handlers/` documents the intended extension point for a later libopenmpt/ffmpeg-backed player module.

## Build on Windows

Install .NET SDK 8 or newer, then run:

```bat
build_windows.bat
```

Create a self-contained Windows x64 single-file build:

```bat
publish_windows_x64_singlefile.bat
```

## Build on Linux

```bash
chmod +x build_linux.sh publish_linux_x64_singlefile.sh run_linux_debug.sh
./build_linux.sh
```

Create a self-contained Linux x64 build:

```bash
./publish_linux_x64_singlefile.sh
```
