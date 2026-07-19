# Playwright Browser Installation Guide

## Problem
Playwright requires Chromium browser to be installed before the web scraping service can function. The error message indicates the browser executable is missing.

## Solution

### Option 1: Automatic Installation (Recommended)
The ExtractService now includes automatic browser installation. When you first call the scraping endpoint, it will attempt to install the browser automatically.

```csharp
POST /api/extract/scrape?keyword=developer&location=NewYork&pages=1
```

The service will:
1. Check if Chromium browser is installed
2. If not found, run `dotnet tool run microsoft.playwright.cli -- install chromium`
3. Proceed with scraping once installation is complete

### Option 2: Manual Installation via PowerShell

**Note:** PowerShell 5.1 or later is required.

#### Step 1: Ensure Playwright CLI is installed
```powershell
dotnet tool install --global Microsoft.Playwright.CLI
```

#### Step 2: Install Chromium browser
```powershell
dotnet tool run microsoft.playwright.cli -- install chromium --with-deps
```

This will download and install Chromium (~300MB) to:
```
C:\Users\[YourUsername]\.playwright\chromium-1217
```

#### Step 3: Verify installation
```powershell
Test-Path "$env:USERPROFILE\.playwright\chromium-1217"
```

Should return `True` if successful.

### Option 3: Manual Installation via Command Line (Windows)

```batch
REM Install CLI tool
dotnet tool install --global Microsoft.Playwright.CLI

REM Install Chromium with dependencies
dotnet tool run microsoft.playwright.cli -- install chromium --with-deps
```

## Troubleshooting

### Issue: "chrome-headless-shell.exe" not found
**Solution:** Run the installation command again:
```powershell
dotnet tool run microsoft.playwright.cli -- install chromium --with-deps
```

### Issue: Installation times out
**Solution:** The initial download can take 5-15 minutes depending on internet speed. Use `--with-deps` flag for full dependency installation:
```powershell
$env:PLAYWRIGHT_BROWSERS_PATH="$env:USERPROFILE\.playwright"
dotnet tool run microsoft.playwright.cli -- install chromium --with-deps
```

### Issue: "Could not find dotnet tool"
**Solution:** Ensure you installed the tool globally:
```powershell
dotnet tool list --global | grep playwright
```

If not found, reinstall:
```powershell
dotnet tool uninstall --global Microsoft.Playwright.CLI
dotnet tool install --global Microsoft.Playwright.CLI
```

## Environment Variables (Optional)

You can customize the Playwright browser installation location:

```powershell
# Set custom browser path (optional)
$env:PLAYWRIGHT_BROWSERS_PATH="C:\CustomPath\browsers"

# Then run install
dotnet tool run microsoft.playwright.cli -- install chromium
```

## Verification

Once installed, verify by checking the directory:

```powershell
Get-ChildItem "$env:USERPROFILE\.playwright" -Recurse | Measure-Object
```

Should show files present in the chromium directory.

## Testing the Extract Service

Once browsers are installed, test the API:

```bash
# Using curl
curl -X POST "https://localhost:7123/api/extract/scrape?keyword=developer&location=NewYork&pages=1" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"

# Response:
{
  "message": "Successfully scraped 25 leads",
  "leads": [
    {
      "id": 1,
      "firstName": "John",
      "lastName": "Smith",
      "email": "john@company.com",
      "phone": "555-1234",
      "company": "Tech Solutions Inc",
      "job": "Manager",
      "location": "New York",
      "industry": "Technology",
      "intent": "Medium"
    }
  ]
}
```

## Performance Notes

- First scrape run: ~30 seconds (browser startup)
- Subsequent runs: ~15-20 seconds per page
- Each page processes ~15-20 businesses
- Built-in random delays to avoid detection: 500-1500ms per business, 1-3s between pages

## Architecture

The ExtractService workflow:

```
API Request → Validate Input → Browser Check → Install if needed → 
Scrape Yellow Pages → Extract Details → Infer Industry → 
Check Duplicates → Save to Database → Return Results
```

For more information, see the ExtractService and ExtractController documentation comments.
