using Microsoft.Extensions.Configuration;
using Serilog;
using System.Collections.Generic;
using AFConfiguration = Sahadeva.Dossier.Common.Configuration;

namespace Sahadeva.Dossier.Common.Logging
{
    public class LoggerFactory
    {
        public static ILogger CreateLogger(string applicationName, string loggerName)
        {
            var inMemorySettings = new Dictionary<string, string>();

            // Retrieve the "WriteTo" section and modify the file path to include the logger name
            var writeToSection = AFConfiguration.ConfigurationManager.Settings.GetSection("Serilog:WriteTo");
            foreach (var writeTo in writeToSection.GetChildren())
            {
                var nameSection = writeTo.GetSection("Name");
                if (nameSection.Value == "File")
                {
                    var filePath = writeTo.GetSection("Args:path");
                    if (filePath.Value != null)
                    {
                        inMemorySettings.Add(filePath.Path, filePath.Value.Replace("{LOGGER_NAME}", loggerName));
                    }
                }
                else if (nameSection.Value == "Conditional")
                {
                    var filePath = writeTo.GetSection("Args:configureSink:0:Args:Path");
                    if (filePath.Value != null)
                    {
                        inMemorySettings.Add(filePath.Path, filePath.Value.Replace("{LOGGER_NAME}", loggerName));
                    }
                }
            }

            var configuration = new ConfigurationBuilder()
                .AddConfiguration(AFConfiguration.ConfigurationManager.Settings)
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var serilogConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("logger", loggerName)
                .Enrich.WithProperty("appname", applicationName)
                .Enrich.WithProperty("source", "adfactors-insights"); // only log messages with this key will be ingested

            return serilogConfig.CreateLogger();
        }
    }
}
