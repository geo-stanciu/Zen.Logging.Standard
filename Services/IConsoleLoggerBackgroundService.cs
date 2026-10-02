using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Logging.Standard.Services
{
    public interface IConsoleLoggerBackgroundService
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
