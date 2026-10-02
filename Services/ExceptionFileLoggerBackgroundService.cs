using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Logging.Standard.Services
{
    public class ExceptionFileLoggerBackgroundService : BaseFileLoggerBackgroundService
    {
        public ExceptionFileLoggerBackgroundService(
            ILoggerService loggerService,
            ILoggingConfigurationService configurationService)
            : base(configurationService)
        {
            _logFileNamePrefix = "exceptions";
            _logQueue = loggerService.ExceptionLogQueue;
        }
    }
}
