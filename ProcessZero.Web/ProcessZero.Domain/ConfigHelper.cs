using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain
{
    public static class ConfigHelper
    {
        public static IConfigurationRoot LoadApiConfiguration()
        {
            // Get the solution root folder (assuming this project is under the solution folder)
            var solutionFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

            // API project folder name
            var apiFolder = Path.Combine(solutionFolder, "ProcessZero.Web");

            // Check that folder exists
            if (!Directory.Exists(apiFolder))
                throw new DirectoryNotFoundException($"API project folder not found at {apiFolder}");

            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(apiFolder)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            return config;
        }
    }
}
