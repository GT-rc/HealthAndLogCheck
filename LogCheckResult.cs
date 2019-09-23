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

//foreach (var item in logCheckResults)
//{
//    sb.Append("Log item ID: ");
//    sb.Append("<b>" + item.LogId.ToString() + "</b>");
//    sb.Append(" at the urgency level of ");
//    sb.Append("<em>" + item.Level + "</em>");
//    sb.Append("<br />");
//    sb.Append("Was added to the logs at: ");
//    sb.Append("<em>" + item.TimeStamp.ToString() + "</em>");
//    sb.Append("<br />The message was: ");
//    sb.Append("<em>" + item.Message + "</em>");
//    sb.Append("<br /><br />");
//    sb.Append("If any exceptions occurred, they will appear below:<br /><br />");
//    sb.Append(item.Exception);
//    sb.Append("<br />-----------------------------------------<br /><br />");   
//}
