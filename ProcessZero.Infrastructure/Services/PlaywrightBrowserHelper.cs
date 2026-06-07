using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Helper class to manage Playwright browser installation.
    /// Ensures Chromium browser is available before attempting to scrape.
    /// </summary>
    public static class PlaywrightBrowserHelper
    {
        private static bool _browserChecked = false;
        private static bool _browserAvailable = false;

        /// <summary>
        /// Ensures Playwright Chromium browser is installed.
        /// Runs the playwright CLI install command if browsers are not found.
        /// </summary>
        public static async Task EnsureBrowsersInstalledAsync()
        {
            if (_browserChecked)
                return;

            _browserChecked = true;

            try
            {
                // Check if browser path exists
                var playwrightPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".playwright"
                );

                var chromiumPath = Path.Combine(playwrightPath, "chromium-1217");

                if (Directory.Exists(chromiumPath))
                {
                    _browserAvailable = true;
                    Console.WriteLine("✓ Playwright Chromium browser found.");
                    return;
                }

                Console.WriteLine("⚠ Playwright Chromium browser not found. Attempting to install...");

                // Try to install using dotnet tool
                var result = await RunPlaywrightInstallAsync();

                if (result)
                {
                    _browserAvailable = true;
                    Console.WriteLine("✓ Playwright Chromium browser installed successfully.");
                }
                else
                {
                    throw new InvalidOperationException(
                        "Failed to install Playwright browsers. " +
                        "Please run manually: dotnet tool run microsoft.playwright.cli -- install chromium");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Browser setup warning: {ex.Message}");
                _browserAvailable = false;
            }
        }

        /// <summary>
        /// Runs the Playwright CLI install command.
        /// </summary>
        private static async Task<bool> RunPlaywrightInstallAsync()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "tool run microsoft.playwright.cli -- install chromium --with-deps",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                        return false;

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    process.WaitForExit(120000); // 2 minute timeout

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine("Playwright install completed successfully.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Playwright install failed with exit code {process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                            Console.WriteLine($"Error: {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during browser installation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets whether browsers are available for use.
        /// </summary>
        public static bool IsBrowserAvailable => _browserAvailable;
    }
}
