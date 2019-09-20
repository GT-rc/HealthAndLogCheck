using System;
using System.Collections.Generic;
using System.Text;

namespace HealthAndLogCheck
{
    public class ApiConnectionCheckResult
    {
        public string Message { get; set; }
        public string IpAddress { get; set; }
        public long RoundTripTime { get; set; }
        public int TimeToLive { get; set; }
        public bool IsFragmented { get; set; }
        public int BufferSize { get; set; }
        public string Error { get; set; }
        public DateTime CheckedOn { get; set; }
    }
}
