using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Standard.Models
{
    public class LogCleanupModel
    {
        public LogCleanupSettingsModel? ExceptionsLog { get; set; }
        public LogCleanupSettingsModel? Log { get; set; }
    }
}
