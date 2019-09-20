using System;
using System.Collections.Generic;
using System.Text;

namespace HealthAndLogCheck
{
    public class LogCheckResult
    {
        public int LogId { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Exception { get; set; }
        public string Properties { get; set; }
        public string AdditionalMessage { get; set; }

    }
}
