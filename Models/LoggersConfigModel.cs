using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Standard.Models
{
    public class LoggersConfigModel
    {
        public bool ConsoleLogger { get; set; }
        public bool MessageQueueLogger { get; set; }
        public bool FileLogger { get; set; }
    }
}
