using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Logging.Standard.Services
{
    public class FileLoggerBackgroundService : BaseFileLoggerBackgroundService
    {
        public FileLoggerBackgroundService(
            ILoggerService loggerService,
            ILoggingConfigurationService configurationService)
            : base(configurationService)
        {
            _logFileNamePrefix = "log";
            _logQueue = loggerService.FileLogQueue;
        }
    }
}
