using System;
using System.Collections.Generic;
using System.Text;
using Zen.Logging.Standard.Models;

namespace Zen.Logging.Standard.Services
{
    public class ConsoleLoggerBackgroundService : IConsoleLoggerBackgroundService
    {
        private ILoggerService _loggerService;

        public ConsoleLoggerBackgroundService(ILoggerService loggerService)
        {
            _loggerService = loggerService;
        }

        private Task RunAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested
                       || (_loggerService.ConsoleLogQueue.Reader.TryPeek(out _) && _loggerService.ConsoleLogQueue.Reader.Count > 0))
                {
                    if (!_loggerService.ConsoleLogQueue.Reader.TryPeek(out _))
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    int nrItems = _loggerService.ConsoleLogQueue.Reader.Count;
                    if (nrItems == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    if (nrItems > 256)
                        nrItems = 256;

                    List<LogMessageModel> logMessages = new List<LogMessageModel>(nrItems);

                    for (int i = 0; i < nrItems; i++)
                    {
                        if (_loggerService.ConsoleLogQueue.Reader.TryRead(out LogMessageModel? msg))
                        {
                            if (msg == null)
                                continue;

                            logMessages.Add(msg);
                        }
                        else
                        {
                            await Task.Delay(10);
                            break;
                        }
                    }

                    if (logMessages.Count == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    foreach (LogMessageModel logModel in logMessages)
                    {
                        ConsoleColor originalColor = Console.ForegroundColor;

                        Console.ForegroundColor = logModel.consoleColor;

                        string utc = logModel.log_time.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                        Console.WriteLine($"{utc} [{logModel.eventId,2}: {logModel.logLevel,-12}]");

                        Console.ForegroundColor = originalColor;

                        if (!string.IsNullOrEmpty(logModel.logName))
                            Console.WriteLine($"     {logModel.logName} - {logModel.message}");
                        else
                            Console.WriteLine($"     {logModel.message}");

                        if (!string.IsNullOrEmpty(logModel.exception_message))
                            Console.WriteLine(logModel.exception_message);
                    }
                }
            });
        }

        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                Task logggerTask = RunAsync(stoppingToken);

                await logggerTask;
            });
        }
    }
}
