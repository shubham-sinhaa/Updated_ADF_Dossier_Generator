using Serilog;
using Serilog.Context;
using System;
using System.Diagnostics;

namespace Sahadeva.Dossier.Common.Logging
{
    public static class LoggerExtensions
    {
        public static TimeLog TrackTime(this ILogger logger, string step)
        {
            return new TimeLog(logger, step);
        }
    }

    public class TimeLog : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _step;
        private readonly Stopwatch _stopWatch;
        private readonly IDisposable _logContext;

        internal TimeLog(ILogger logger, string step)
        {
            _logger = logger;
            _step = step;
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            _logContext = LogContext.PushProperty("step", step);
        }

        public void Dispose()
        {
            _stopWatch.Stop();

            using (LogContext.PushProperty("messageType", "TimeLog"))
            {
                _logger.Information("Finished processing step '{step}' in {executionTime} seconds", _step, _stopWatch.Elapsed.TotalSeconds);
            }

            _logContext.Dispose();
        }
    }
}
