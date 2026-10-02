using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Zen.Logging.Extensions;
using Zen.Logging.Models;

namespace Zen.Logging.Standard.Services
{
    public class LoggerService : ILoggerService
    {
        private readonly ILoggingConfigurationService _configurationService;

        private bool _isConsoleLogger = true;
        private bool _isMessageQueueLogger = false;
        private bool _isFileLogger = false;

        private LogLevel _loglevel = LogLevel.Information;

        public Channel<LogMessageModel> Queue { get; private set; } = Channel.CreateUnbounded<LogMessageModel>();
        public Channel<LogMessageModel> ConsoleLogQueue { get; private set; } = Channel.CreateUnbounded<LogMessageModel>();
        public Channel<LogMessageModel> FileLogQueue { get; private set; } = Channel.CreateUnbounded<LogMessageModel>();
        public Channel<LogMessageModel> ExceptionLogQueue { get; private set; } = Channel.CreateUnbounded<LogMessageModel>();

        public LoggerService(ILoggingConfigurationService configurationService)
        {
            _configurationService = configurationService;

            _isMessageQueueLogger = _configurationService.GetMessageQueueLogger();
            _isFileLogger = _configurationService.GetFileLogger();

            _loglevel = GetDefaultLogLevel();
        }

        private LogLevel GetDefaultLogLevel()
        {
            string logLevel = _configurationService.GetDefaultLogLevel();
            return (LogLevel)Enum.Parse(typeof(LogLevel), logLevel);
        }

        private void Log(LogLevel logLevel, ConsoleColor logColor, string message, string exceptionMessage = "")
        {
            if ((int)logLevel < (int)_loglevel)
                return;

            LogMessageModel logMessage = new LogMessageModel
            {
                consoleColor = logColor,
                logLevel = logLevel,
                logName = "",
                source = _configurationService.GetLogSource(),
                source_version = _configurationService.GetLogSourceVersion(),
                ip = GetLocalIPv4(),
                log_time = DateTime.UtcNow,
                message = message,
                exception_message = exceptionMessage
            };

            bool queueLoggerSuccess = false;
            bool consoleLoggerSuccess = false;
            bool fileLoggerSuccess = false;
            bool exceptionLoggerSuccess = false;
            bool exceptionLogger = _isFileLogger && (logMessage.logLevel == LogLevel.Error || !string.IsNullOrEmpty(logMessage.exception_message));
            int retry = 10;

            while (true)
            {
                if (retry <= 0)
                    throw new Exception("logger error - max retries reached for ");

                try
                {
                    if (_isMessageQueueLogger && !queueLoggerSuccess)
                        queueLoggerSuccess = Queue.Writer.TryWrite(logMessage);

                    if (_isConsoleLogger && !consoleLoggerSuccess)
                        consoleLoggerSuccess = ConsoleLogQueue.Writer.TryWrite(logMessage);

                    if (_isFileLogger && !fileLoggerSuccess)
                        fileLoggerSuccess = FileLogQueue.Writer.TryWrite(logMessage);

                    if (exceptionLogger && !exceptionLoggerSuccess)
                        exceptionLoggerSuccess = ExceptionLogQueue.Writer.TryWrite(logMessage);
                }
                catch
                {
                    Thread.Sleep(100);
                }
                finally
                {
                    retry--;
                }

                if ((!_isMessageQueueLogger || queueLoggerSuccess)
                    && (!_isConsoleLogger || consoleLoggerSuccess)
                    && (!_isFileLogger || fileLoggerSuccess)
                    && (!exceptionLogger || exceptionLoggerSuccess))
                {
                    break;
                }
            }
        }

        private string GetLocalIPv4(NetworkInterfaceType type = NetworkInterfaceType.Ethernet)
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .FirstOrDefault(ni =>
                    ni.NetworkInterfaceType == type
                    && ni.OperationalStatus == OperationalStatus.Up
                    && ni.GetIPProperties().GatewayAddresses.FirstOrDefault() != null
                    && ni.GetIPProperties().UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork) != null
                )
                ?.GetIPProperties()
                .UnicastAddresses
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                ?.Address
                ?.ToString()
                ?? string.Empty;
        }

        public void LogDebug(string message)
        {
            Log(LogLevel.Debug, ConsoleColor.Blue, message);
        }

        public void LogInformation(string message)
        {
            Log(LogLevel.Information, ConsoleColor.Green, message);
        }

        public void LogWarning(string message)
        {
            Log(LogLevel.Warning, ConsoleColor.DarkMagenta, message);
        }

        public void LogError(string message)
        {
            Log(LogLevel.Error, ConsoleColor.Red, message);
        }

        public void LogError(Exception ex, string message)
        {
            Log(LogLevel.Error, ConsoleColor.Red, message, ex != null ? ex.GetMessageWithStackTrace() : "");
        }
    }
}
