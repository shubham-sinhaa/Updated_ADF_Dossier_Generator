using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Sahadeva.Dossier.Common.Configuration
{
    public static class ConfigurationManager
    {
        public static IConfiguration Settings { get; private set; }

        static ConfigurationManager()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")}.json", optional: true)
            .AddEnvironmentVariables();

            Settings = builder.Build();
        }
    }
}
