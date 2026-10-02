using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Threading.Channels;
using Zen.Logging.Models;

namespace Zen.Logging.Standard.Services
{
    public abstract class BaseFileLoggerBackgroundService : IBaseFileLoggerBackgroundService
    {
        private ILoggingConfigurationService _configurationService;

        protected string? _logFileNamePrefix = null;

        protected Channel<LogMessageModel>? _logQueue;

        private static object _deleteOldLogsLockObject = new object();

        public BaseFileLoggerBackgroundService(
            ILoggingConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        private string GetLoggingDirectoryName(string date)
        {
            return _configurationService.GetLogDirectory()
                .Replace("%Date%", date) ?? "./";
        }

        private string GetLoggingFileName(string directory, ref string fileSeq, ref int fileNumber)
        {
            string fileName = Path.Combine(directory, $"{_logFileNamePrefix}{fileSeq}.txt");

            if (!File.Exists(fileName))
                return fileName;

            FileInfo fi = new FileInfo(fileName);

            if (fi.Length < 5000000)
                return fileName;

            fileNumber++;
            fileSeq = $"_{fileNumber:0000}";

            return Path.Combine(directory, $"{_logFileNamePrefix}{fileSeq}.txt");
        }

        private void DeleteOldLogs(int keepDays, CancellationToken stoppingToken)
        {
            string today = $"{DateTime.UtcNow:yyyy-MM-dd}";
            string loggingPath = new DirectoryInfo(GetLoggingDirectoryName(today))?.Parent?.FullName ?? "";

            if (string.IsNullOrEmpty(loggingPath))
                return;

            DateTime cutOff = DateTime.UtcNow.AddDays(-1 * keepDays);

            string[] logDirectories = Directory.GetDirectories(loggingPath);

            foreach (string logDirectory in logDirectories)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                string logDirectoryName = new DirectoryInfo(logDirectory).Name;

                if (logDirectoryName == today)
                    continue;
                else if (DateTime.ParseExact(logDirectoryName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) >= cutOff.Date)
                    continue;

                Directory.Delete(logDirectory, true);
            }
        }

        private void ArchiveOldLogFiles(CancellationToken stoppingToken)
        {
            string today = $"{DateTime.UtcNow:yyyy-MM-dd}";
            string loggingPath = new DirectoryInfo(GetLoggingDirectoryName(today))?.Parent?.FullName ?? "";

            if (string.IsNullOrEmpty(loggingPath))
                return;

            string[] logDirectories = Directory.GetDirectories(loggingPath);

            foreach (string logDirectory in logDirectories)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                string logDirectoryName = new DirectoryInfo(logDirectory).Name;

                if (logDirectoryName == today)
                    continue;

                string[] logFiles = Directory.GetFiles(logDirectory, $"{_logFileNamePrefix}*.txt");

                if (logFiles.Length == 0)
                    continue;

                CreateLogArchive(logFiles, logDirectory, $"{_logFileNamePrefix}_{logDirectoryName}");
                DeleteLogFiles(logFiles);
            }
        }

        private void DeleteLogFiles(string[] logFiles)
        {
            foreach (string logFile in logFiles)
            {
                File.Delete(logFile);
            }
        }

        private void CreateLogArchive(string[] logFiles, string logDirectory, string baseName)
        {
            string archiveName = Path.Combine(logDirectory, $"{baseName}.zip");

            using (FileStream zipStream = new FileStream(archiveName, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (string logFile in logFiles)
                    {
                        ZipArchiveEntry entry = archive.CreateEntry(new FileInfo(logFile).Name);
                        using (Stream entryStream = entry.Open())
                        {
                            byte[] buffer = File.ReadAllBytes(logFile);
                            entryStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
        }

        private Task LogCleanupAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                if (_configurationService == null)
                    return;

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_configurationService.GetLogKeepDays() > 0)
                    {
                        lock (_deleteOldLogsLockObject)
                        {
                            DeleteOldLogs(_configurationService.GetLogKeepDays(), stoppingToken);
                        }
                    }

                    if (_configurationService.GetLogArchiveLogFiles())
                    {
                        lock (_deleteOldLogsLockObject)
                        {
                            ArchiveOldLogFiles(stoppingToken);
                        }
                    }

                    await Task.Delay(3600 * 1000, stoppingToken);
                }
            });
        }

        private Task LogAsync(CancellationToken stoppingToken)
        {
            if (_logQueue == null)
                throw new NullReferenceException(nameof(_logQueue));

            return Task.Run(async () =>
            {
                string lastDay = string.Empty;
                string loggingDirectory = string.Empty;
                int fileNumber = 0;
                string fileSeq = $"_{fileNumber:0000}";

                while (!stoppingToken.IsCancellationRequested
                       || (_logQueue.Reader.TryPeek(out _) && _logQueue.Reader.Count > 0))
                {
                    if (!_logQueue.Reader.TryPeek(out _))
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    int nrItems = _logQueue.Reader.Count;
                    if (nrItems == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    if (nrItems > 256)
                        nrItems = 256;

                    List<LogMessageModel> messages = new List<LogMessageModel>(nrItems);

                    for (int i = 0; i < nrItems; i++)
                    {
                        if (_logQueue.Reader.TryRead(out LogMessageModel? msg))
                        {
                            if (msg == null)
                                continue;

                            messages.Add(msg);
                        }
                        else
                        {
                            await Task.Delay(10);
                            break;
                        }
                    }

                    if (messages.Count == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    string today = $"{DateTime.UtcNow:yyyy-MM-dd}";

                    if (today != lastDay)
                    {
                        lastDay = today;
                        loggingDirectory = GetLoggingDirectoryName(today);
                        Directory.CreateDirectory(loggingDirectory);
                    }

                    string fileName = GetLoggingFileName(loggingDirectory, ref fileSeq, ref fileNumber);

                    StringBuilder sb = new StringBuilder();

                    foreach (LogMessageModel logModel in messages)
                    {
                        string utc = logModel.log_time.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                        sb.AppendLine($"{utc} [{logModel.eventId,2}: {logModel.logLevel,-12}]");
                        sb.AppendLine($"     {logModel.logName} - {logModel.message}");

                        if (!string.IsNullOrEmpty(logModel.exception_message))
                            sb.AppendLine(logModel.exception_message);
                    }

                    File.AppendAllText(fileName, sb.ToString());

                    messages.Clear();
                    await Task.Delay(100);
                }
            });
        }

        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                Task logCleanupTask = LogCleanupAsync(stoppingToken);
                Task logggerTask = LogAsync(stoppingToken);

                await logggerTask;
                await logCleanupTask;
            });
        }
    }
}
