using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using Zen.Logging.Models;

namespace Zen.Logging.Standard.Services
{
    public interface ILoggerService
    {
        Channel<LogMessageModel> Queue { get; }
        Channel<LogMessageModel> ConsoleLogQueue { get; }
        Channel<LogMessageModel> FileLogQueue { get; }
        Channel<LogMessageModel> ExceptionLogQueue { get; }

        void LogDebug(string message);
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(Exception ex, string message);
    }
}
