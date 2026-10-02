using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Logging.Standard.Services
{
    public interface IBaseFileLoggerBackgroundService
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
