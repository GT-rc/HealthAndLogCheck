using System;
using System.Collections.Generic;
using System.Text;

namespace HealthAndLogCheck
{
    public class ConnectionCheckResult
    {
        public string Message { get; set; }

        public string DbName { get; set; }

        public bool IsConnected { get; set; }

        public DateTime LastCheckedOn { get; set; }

        public string Errors { get; set; }
    }
}
