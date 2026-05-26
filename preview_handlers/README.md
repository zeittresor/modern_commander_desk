# Preview handlers

Built-in preview handler source code lives in `src/PreviewHandlers/`.

Runtime extension ideas for later versions:

- `mod_player/` using libopenmpt or an external command-line player for MOD/XM/S3M/IT files
- `ffmpeg_media_preview/` for audio/video metadata and thumbnails
- `image_extras/` for unusual image formats
- `hex_plus/` for richer binary inspection

v0.3 includes built-in handlers for text/code, images, tracker module metadata, audio/video metadata placeholders, and hex fallback.
