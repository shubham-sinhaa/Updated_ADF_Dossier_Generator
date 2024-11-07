using Amazon.Runtime;
using Amazon.S3;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sahadeva.Dossier.Common.Logging;
using Sahadeva.Dossier.DAL;
using Sahadeva.Dossier.DocumentGenerator.Configuration;
using Sahadeva.Dossier.DocumentGenerator.Data;
using Sahadeva.Dossier.DocumentGenerator.Formatters;
using Sahadeva.Dossier.DocumentGenerator.Imaging;
using Sahadeva.Dossier.DocumentGenerator.IO;
using Sahadeva.Dossier.DocumentGenerator.Messaging;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using Sahadeva.Dossier.DocumentGenerator.Parsers;
using Sahadeva.Dossier.DocumentGenerator.Processors;
using Sahadeva.Dossier.Entities;
using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using ConfigurationManager = Sahadeva.Dossier.Common.Configuration.ConfigurationManager;

namespace Sahadeva.Dossier.DocumentGenerator
{
    internal class Program
    {
        static readonly IHost _appHost = InitialiseHost();

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

            var sqsClient = _appHost.Services.GetRequiredService<IJobFetcher>();

            // we want this to keep running and processing jobs as they become available
            while (true)
            {
                DossierJob? job = null;
                try
                {
                    Log.Debug($"Begin fetch from SQS");
                    job = await sqsClient.ReceiveMessage();
                    Log.Debug($"Fetch from SQS complete.");

                    if (job != null)
                    {
                        using (LogContext.PushProperty("runId", job.RunId))
                        using (LogContext.PushProperty("DID", job.DID))
                        {
                            var timer = new Stopwatch();
                            timer.Start();

                            var dossierGenerator = _appHost.Services.GetRequiredService<DossierGenerator>();
                            await dossierGenerator.ExecuteJob(job);

                            timer.Stop();
                            Log.Information($"Finished generating dossier {job.OutputFilePath} in {timer.Elapsed.TotalSeconds} seconds.");
                        }
                    }
                    else
                    {
                        Log.Verbose($"Did not get any messages. Waiting for 60 secs");
                        await Task.Delay(60 * 1000);
                    }
                }
                catch (Exception ex)
                {
                    var logWithContext = job == null ?
                        Log.Logger :
                        Log.ForContext("runId", job)
                           .ForContext("DID", job.DID)
                           .ForContext("template", job.TemplateName);

                    logWithContext.Error(ex, ex.Message);
                }
            }
        }

        private static bool RunningInContainer => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        private static void ConfigureLogger() => Log.Logger = LoggerFactory.CreateLogger("dossier-document-generator", "Main");

        /// <summary>
        /// Bootstraps the application
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        static IHost InitialiseHost()
        {
            return Host.CreateDefaultBuilder()
            .ConfigureServices(GetApplicationServices)
            .Build();
        }

        static void GetApplicationServices(HostBuilderContext context, IServiceCollection services)
        {
            var configuration = ConfigurationManager.Settings;

            ConfigureStorageProvider(services, configuration);

            services.AddSingleton<DocumentHelper>();
            services.AddSingleton<PlaceholderHelper>();
            services.AddSingleton<PlaceholderParser>();
            services.AddSingleton<PlaceholderFactory>();
            services.AddSingleton<FormatterFactory>();
            services.AddSingleton<RowPlaceholderFactory>();
            services.AddSingleton<DatasetLoader>();
            services.AddSingleton<ScreenshotService>();
            services.AddSingleton<GraphService>();
            services.AddSingleton<DossierDAL>();
            services.AddSingleton<ImageDownloader>();

#if DEBUG
            // this service bypasses the SQS queue and directly fetches the first available job from the DB
            // used for debugging purposes only
            services.AddSingleton<IJobFetcher, DevJobFetcher>();
#else
            // Release mode implementation
            services.AddSingleton<IJobFetcher, SQSJobFetcher>();
#endif

            services.AddTransient<DossierGenerator>();

            services.AddOptions<ScreenshotOptions>().Bind(configuration.GetSection(ScreenshotOptions.ConfigKey));
            services.AddOptions<GraphOptions>().Bind(configuration.GetSection(GraphOptions.ConfigKey));
        }

        private static void ConfigureStorageProvider(IServiceCollection services, IConfiguration configuration)
        {
            var storageProvider = configuration.GetRequiredSection("Storage").GetValue<StorageProvider>("Provider");

            if (storageProvider == StorageProvider.Filesystem)
            {
                services.Configure<TemplateStorageOptions>(configuration.GetRequiredSection("Storage:Options"));
                services.AddSingleton<IStorageProvider, FilesystemStorageProvider>();
            }
            else if (storageProvider == StorageProvider.S3)
            {
                var awsOptions = configuration.GetAWSOptions("AWS");
                var accessKey = configuration["AWS:AccessKey"];
                var secretKey = configuration["AWS:SecretKey"];
                if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
                {
                    awsOptions.Credentials = new BasicAWSCredentials(accessKey, secretKey);
                }

                services.AddDefaultAWSOptions(awsOptions);
                services.AddAWSService<IAmazonS3>();

                services.Configure<S3StorageOptions>(configuration.GetRequiredSection("Storage:Options"));
                
                // Manually bind IOptions<TemplateStorageOptions> to IOptions<S3StorageOptions>
                services.AddSingleton<IOptions<TemplateStorageOptions>>(sp =>
                    sp.GetRequiredService<IOptions<S3StorageOptions>>());

                services.AddSingleton<IStorageProvider, S3StorageProvider>();
            }
            else
            {
                throw new ApplicationException($"Storage provider could not be found. Please check config 'Storage:Provider'");
            }
        }

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
