#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Quick Playwright Browser Setup Script for Windows PowerShell

.DESCRIPTION
    Installs Playwright CLI and Chromium browser for the Extract Service

.EXAMPLE
    .\setup-playwright.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Playwright Browser Setup" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if dotnet is installed
Write-Host "[1/3] Checking for dotnet..." -ForegroundColor Yellow
$dotnetVersion = & dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet CLI is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "✓ dotnet CLI found: $dotnetVersion" -ForegroundColor Green

Write-Host ""
Write-Host "[2/3] Installing Playwright CLI tool..." -ForegroundColor Yellow
$installResult = & dotnet tool install --global Microsoft.Playwright.CLI --ignore-failed-sources 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Could not install CLI, attempting to update..." -ForegroundColor Yellow
    $updateResult = & dotnet tool update --global Microsoft.Playwright.CLI --ignore-failed-sources 2>&1
}
Write-Host "✓ Playwright CLI installed/updated" -ForegroundColor Green

Write-Host ""
Write-Host "[3/3] Installing Chromium browser..." -ForegroundColor Yellow
Write-Host "This may take 5-15 minutes. Please wait..." -ForegroundColor Yellow
Write-Host ""

& dotnet tool run microsoft.playwright.cli -- install chromium --with-deps

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "====================================" -ForegroundColor Green
    Write-Host "✓ SUCCESS: Playwright setup complete!" -ForegroundColor Green
    Write-Host "====================================" -ForegroundColor Green
    Write-Host ""

    $browserPath = Join-Path $env:USERPROFILE ".playwright\chromium-1217"
    Write-Host "Browser installed at:" -ForegroundColor Green
    Write-Host "  $browserPath" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "You can now run the Extract Service API endpoint:" -ForegroundColor Green
    Write-Host "  POST /api/extract/scrape?keyword=developer&location=NewYork&pages=1" -ForegroundColor Cyan
    Write-Host ""

    # Verify installation
    if (Test-Path $browserPath) {
        $items = @(Get-ChildItem $browserPath -Recurse -ErrorAction SilentlyContinue)
        Write-Host "Browser verification: $($items.Count) files found ✓" -ForegroundColor Green
    }
} else {
    Write-Host ""
    Write-Host "====================================" -ForegroundColor Red
    Write-Host "✗ ERROR: Failed to install Chromium" -ForegroundColor Red
    Write-Host "====================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please try running manually:" -ForegroundColor Yellow
    Write-Host "  dotnet tool run microsoft.playwright.cli -- install chromium --with-deps" -ForegroundColor Cyan
    Write-Host ""
}

Read-Host "Press Enter to exit"
