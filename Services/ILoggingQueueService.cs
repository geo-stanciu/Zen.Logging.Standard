using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Zen.Logging.Standard.Models;

namespace Zen.Logging.Standard.Services
{
    public interface ILoggingQueueService
    {
        Channel<LogMessageModel> queue { get; set; }
        Channel<LogMessageModel> consoleLoggingQueue { get; set; }
        Channel<LogMessageModel> exceptionLoggingQueue { get; set; }
        Channel<LogMessageModel> fileLoggingQueue { get; set; }
        void Add(LogMessageModel logMessage);
    }
}
