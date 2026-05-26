#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

echo ""
echo "============================================================"
echo " Modern Commander Desk v0.4.5 - Linux x64 Single-File Publish"
echo "============================================================"
echo ""

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[ERROR] .NET SDK not found. Install .NET 8 SDK or newer, then run this again."
  exit 1
fi

dotnet restore ModernCommanderDesk.csproj

dotnet publish ModernCommanderDesk.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishTrimmed=false \
  -o dist/linux-x64-singlefile

chmod +x dist/linux-x64-singlefile/ModernCommanderDesk || true

echo ""
echo "[OK] Output:"
echo "  dist/linux-x64-singlefile/ModernCommanderDesk"
