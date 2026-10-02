using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Logging.Standard.Services
{
    public interface ILoggingConfigurationService
    {
        string GetDefaultLogLevel();

        bool GetMessageQueueLogger();

        bool GetFileLogger();

        string GetLogSource();

        string GetLogSourceVersion();

        string GetLogDirectory();

        int GetLogKeepDays();

        bool GetLogArchiveLogFiles();
    }
}
