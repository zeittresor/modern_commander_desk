#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

echo ""
echo "============================================================"
echo " Modern Commander Desk v0.3.3 - Linux Build"
echo "============================================================"
echo ""

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[ERROR] .NET SDK not found. Install .NET 8 SDK or newer, then run this again."
  exit 1
fi

dotnet --version

echo ""
echo "[1/2] Restoring NuGet packages..."
dotnet restore ModernCommanderDesk.csproj

echo ""
echo "[2/2] Building Release..."
dotnet build ModernCommanderDesk.csproj -c Release --no-restore

echo ""
echo "[OK] Run with:"
echo "  dotnet bin/Release/net8.0/ModernCommanderDesk.dll"
