@echo off
setlocal
cd /d "%~dp0"

echo.
echo ============================================================
echo  Modern Commander Desk v0.4.5 - Windows x64 Framework-Dependent Publish
echo ============================================================
echo.

echo This creates a smaller output, but the target machine needs the .NET Desktop Runtime.
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] .NET SDK not found.
    pause
    exit /b 1
)

dotnet restore ModernCommanderDesk.csproj
if errorlevel 1 goto :fail

dotnet publish ModernCommanderDesk.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained false ^
  -o dist\win-x64-framework-dependent
if errorlevel 1 goto :fail

echo.
echo [OK] Output:
echo   dist\win-x64-framework-dependent\ModernCommanderDesk.exe
echo.
pause
exit /b 0

:fail
echo.
echo [ERROR] Publish failed.
pause
exit /b 1
