@echo off
REM Quick Playwright Setup Script for Windows
REM Run this script to install Playwright Chromium browser

echo.
echo ====================================
echo Playwright Browser Setup
echo ====================================
echo.

REM Step 1: Check if dotnet is installed
echo [1/3] Checking for dotnet...
dotnet --version
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet CLI is not installed or not in PATH
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)
echo ✓ dotnet CLI found

echo.
echo [2/3] Installing Playwright CLI tool...
dotnet tool install --global Microsoft.Playwright.CLI --ignore-failed-sources
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: Could not install CLI, attempting to update existing installation...
    dotnet tool update --global Microsoft.Playwright.CLI --ignore-failed-sources
)
echo ✓ Playwright CLI installed/updated

echo.
echo [3/3] Installing Chromium browser...
echo This may take 5-15 minutes. Please wait...
echo.
dotnet tool run microsoft.playwright.cli -- install chromium --with-deps
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ====================================
    echo ✓ SUCCESS: Playwright setup complete!
    echo ====================================
    echo.
    echo Browser installed at:
    echo %USERPROFILE%\.playwright\chromium-1217
    echo.
    echo You can now run the Extract Service API endpoint:
    echo POST /api/extract/scrape?keyword=developer^&location=NewYork^&pages=1
    echo.
) else (
    echo.
    echo ====================================
    echo ✗ ERROR: Failed to install Chromium
    echo ====================================
    echo.
    echo Please try running manually:
    echo   dotnet tool run microsoft.playwright.cli -- install chromium --with-deps
    echo.
)

pause
