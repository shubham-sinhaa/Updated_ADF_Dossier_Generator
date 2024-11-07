using DotNetEnv;
using Sahadeva.Dossier.Common.Configuration;
using Sahadeva.Dossier.Common.Logging;
using Serilog;
using Serilog.Context;
using System.Reflection;
using System.Text;

namespace Sahadeva.Dossier.JobGenerator
{
    internal class Program
    {
        private const int DefaultPollingIntervalInSeconds = 120;

        static async Task Main(string[] args)
        {
            if (!RunningInContainer)
            {
                Env.Load();
            }

            ConfigureLogger();

            if (bool.TryParse(ConfigurationManager.Settings[ConfigKeys.DEBUG_ENV], out bool debug) && debug)
            {
                PrintSettings();
            }

            Log.Information("Dossier Job Generator started");

            var pollingIntervalInSeconds = int.Parse(ConfigurationManager.Settings[ConfigKeys.PollingIntervalInSeconds] ?? DefaultPollingIntervalInSeconds.ToString());
            var sqsClient = new SQSClient();

            while (true)
            {
                // this is a correlation id that can be used to find all the log messages that are generated for this specific run
                var runId = Guid.NewGuid().ToString();

                using (LogContext.PushProperty("runId", runId))
                {
                    try
                    {
                        var jobs = DossierJobGenerator.GetPendingJobs(runId);
                        await sqsClient.SendBatchRequest(jobs);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, ex.Message);
                    }

                    Log.Verbose("Next check in {pollingInteval} seconds", pollingIntervalInSeconds);
                    await Task.Delay(pollingIntervalInSeconds * 1000);
                }
            }
        }

        private static bool RunningInContainer => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        private static void ConfigureLogger() => Log.Logger = LoggerFactory.CreateLogger("dossier-job-generator", "DossierJobGenerator");

        private static void PrintSettings()
        {
            FieldInfo[] fields = typeof(ConfigKeys).GetFields(BindingFlags.Public | BindingFlags.Static);
            var settings = new StringBuilder().AppendLine();

            foreach (FieldInfo field in fields)
            {
                var key = (string)field.GetValue(null)!;
                var value = ConfigurationManager.Settings[key] ?? "<not set>";
                settings.AppendLine($"{key}={value}");

            }
            Log.Verbose("Settings: {settings}", settings.ToString());
        }
    }
}
