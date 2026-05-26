# preview_handlers

This folder contains optional JSON manifests for external preview helpers.

v0.4 includes built-in preview routing for text/code, images, audio, video, tracker modules, RTF/DOCX/ODT/PDF metadata and hex fallback. Heavy playback/rendering engines are intentionally not bundled into the core app. Instead the preview window can launch small helper tools such as mpv, VLC, FFplay, openmpt123 or TiMidity++.

Example manifest:

```json
{
  "id": "mpv-audio",
  "name": "mpv audio helper",
  "kind": "audio",
  "extensions": [".mp3", ".ogg", ".flac", ".mod", ".xm"],
  "executable": "mpv",
  "arguments": "--force-window=yes \"{file}\""
}
```

Supported `kind` values used by the built-in handlers: `audio`, `video`, `document`.

The first matching helper that is available in PATH or by absolute executable path is used. If no helper is found, the preview dialog still shows file metadata and offers to open the file with the default system application.
