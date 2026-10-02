using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Zen.Logging.Standard.Services;

namespace Zen.Logging.Standard.Middleware
{
    public static class ConsoleAndFileLoggingMiddleware
    {
        public static Task SetupMiddlewareAsync(
            ILoggingConfigurationService configurationService,
            ILoggerService loggerService,
            CancellationToken cancellationToken)
        {
            IConsoleLoggerBackgroundService consoleLoggerBackgroundService = new ConsoleLoggerBackgroundService(loggerService);
            IBaseFileLoggerBackgroundService fileLoggerBackgroundService = new FileLoggerBackgroundService(loggerService, configurationService);
            IBaseFileLoggerBackgroundService exceptionFileLoggerBackgroundService = new ExceptionFileLoggerBackgroundService(loggerService, configurationService);

            Task consoleLoggerBackgroundServiceTask = Task.Run(async () => await consoleLoggerBackgroundService.ExecuteAsync(cancellationToken));

            Task fileLoggerBackgroundServiceTask = Task.Run(async () =>
            {
                if (!configurationService.GetFileLogger())
                    return;

                await fileLoggerBackgroundService.ExecuteAsync(cancellationToken);
            });

            Task exceptionFileLoggerBackgroundServiceTask = Task.Run(async () =>
            {
                if (!configurationService.GetFileLogger())
                    return;

                await exceptionFileLoggerBackgroundService.ExecuteAsync(cancellationToken);
            });

            return Task.WhenAll(
                consoleLoggerBackgroundServiceTask,
                fileLoggerBackgroundServiceTask,
                exceptionFileLoggerBackgroundServiceTask);
        }
    }
}
