@echo off
setlocal
cd /d "%~dp0"

echo.
echo ============================================================
echo  Modern Commander Desk v0.3.3 - Windows Build
echo ============================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] .NET SDK not found.
    echo Install .NET 8 SDK or newer from Microsoft, then run this again.
    pause
    exit /b 1
)

dotnet --version
if errorlevel 1 goto :fail

echo.
echo [1/3] Restoring NuGet packages...
dotnet restore ModernCommanderDesk.csproj
if errorlevel 1 goto :fail

echo.
echo [2/3] Building Release...
dotnet build ModernCommanderDesk.csproj -c Release --no-restore
if errorlevel 1 goto :fail

echo.
echo [3/3] Done.
echo Run with:
echo   dotnet bin\Release\net8.0\ModernCommanderDesk.dll
echo or publish a standalone EXE with publish_windows_x64_singlefile.bat
echo.
pause
exit /b 0

:fail
echo.
echo [ERROR] Build failed.
pause
exit /b 1
