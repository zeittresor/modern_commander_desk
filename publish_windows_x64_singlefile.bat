@echo off
setlocal
cd /d "%~dp0"

echo.
echo ============================================================
echo  Modern Commander Desk v0.4.5 - Windows x64 Single-File Publish
echo ============================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] .NET SDK not found.
    echo Install .NET 8 SDK or newer from Microsoft, then run this again.
    pause
    exit /b 1
)

echo [1/2] Restoring packages...
dotnet restore ModernCommanderDesk.csproj
if errorlevel 1 goto :fail

echo.
echo [2/2] Publishing self-contained single-file EXE...
dotnet publish ModernCommanderDesk.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:PublishTrimmed=false ^
  -o dist\win-x64-singlefile
if errorlevel 1 goto :fail

echo.
echo [OK] Output:
echo   dist\win-x64-singlefile\ModernCommanderDesk.exe
echo.
pause
exit /b 0

:fail
echo.
echo [ERROR] Publish failed.
pause
exit /b 1
