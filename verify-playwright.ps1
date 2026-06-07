#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verify Playwright Installation

.DESCRIPTION
    Checks if Playwright browser is correctly installed and accessible

.EXAMPLE
    .\verify-playwright.ps1
#>

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Playwright Installation Verification" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check 1: dotnet CLI
Write-Host "[1/5] Checking dotnet CLI..." -ForegroundColor Yellow
$dotnet = & dotnet --version 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ dotnet: $dotnet" -ForegroundColor Green
} else {
    Write-Host "  ✗ dotnet not found" -ForegroundColor Red
}

# Check 2: Playwright CLI
Write-Host "[2/5] Checking Playwright CLI tool..." -ForegroundColor Yellow
$tools = & dotnet tool list --global 2>&1
if ($tools -match "microsoft.playwright.cli") {
    Write-Host "  ✓ Playwright CLI installed" -ForegroundColor Green
} else {
    Write-Host "  ✗ Playwright CLI not installed" -ForegroundColor Red
    Write-Host "    Run: dotnet tool install --global Microsoft.Playwright.CLI" -ForegroundColor Yellow
}

# Check 3: Browser path
Write-Host "[3/5] Checking browser installation path..." -ForegroundColor Yellow
$browserPath = Join-Path $env:USERPROFILE ".playwright"
if (Test-Path $browserPath) {
    Write-Host "  ✓ Browser path exists: $browserPath" -ForegroundColor Green
} else {
    Write-Host "  ✗ Browser path not found: $browserPath" -ForegroundColor Red
    Write-Host "    Run: dotnet tool run microsoft.playwright.cli -- install chromium --with-deps" -ForegroundColor Yellow
}

# Check 4: Chromium executable
Write-Host "[4/5] Checking Chromium executable..." -ForegroundColor Yellow
$chromiumExe = Join-Path $browserPath "chromium-1217" "chrome-headless-shell-win64" "chrome-headless-shell.exe"
if (Test-Path $chromiumExe) {
    Write-Host "  ✓ Chromium executable found" -ForegroundColor Green
    Write-Host "    Path: $chromiumExe" -ForegroundColor Gray
} else {
    Write-Host "  ✗ Chromium executable not found" -ForegroundColor Red
    Write-Host "    Expected at: $chromiumExe" -ForegroundColor Gray
}

# Check 5: File count
Write-Host "[5/5] Counting browser files..." -ForegroundColor Yellow
if (Test-Path $browserPath) {
    $fileCount = @(Get-ChildItem $browserPath -Recurse -ErrorAction SilentlyContinue).Count
    if ($fileCount -gt 0) {
        Write-Host "  ✓ $fileCount files found in browser directory" -ForegroundColor Green
    } else {
        Write-Host "  ✗ No files found in browser directory" -ForegroundColor Red
    }
} else {
    Write-Host "  ⊘ Cannot count files - directory not found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan

# Summary
$allGood = (Test-Path $browserPath) -and (Test-Path $chromiumExe)
if ($allGood) {
    Write-Host "✓ Playwright is ready to use!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run the Extract Service:" -ForegroundColor Green
    Write-Host "  POST /api/extract/scrape?keyword=developer&location=NYC&pages=1" -ForegroundColor Cyan
} else {
    Write-Host "✗ Playwright installation is incomplete" -ForegroundColor Red
    Write-Host ""
    Write-Host "Run this to fix:" -ForegroundColor Yellow
    Write-Host "  dotnet tool run microsoft.playwright.cli -- install chromium --with-deps" -ForegroundColor Cyan
}

Write-Host ""
