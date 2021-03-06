﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using Serilog;
using Serilog.Core;
using System.Net.NetworkInformation;
using System.Data;
using System.IO;
using System.Linq;
using System.Data.Common;

namespace HealthAndLogCheck
{
    public class Program
    {
        /// <summary>
        /// Main entry point for the program.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .ReadFrom.AppSettings()
                .CreateLogger();

            logger.Information("Starting logging.");
            Console.WriteLine("Starting...");
            List<ConnectionCheckResult> dbMessageString = CheckConnections(logger);
            List<ApiConnectionCheckResult> apiMessageString = ServerStatusPing(logger);
            List<List<string>> logCheckResults = GetRecentLogs(logger); 
            Tuple<string, string> logFileResults = GetLogsFromFiles(logger);
            SendDbAndApiUpdateEmails(dbMessageString, apiMessageString, logger);
            SendLogUpdateEmails(logCheckResults, logFileResults, logger);
            Console.WriteLine("Finishing...");
            logger.Information("Program complete.\n\n");
            //logger.CloseAndFlush();  // docs recommended including this, but it's not being recognized
        }

        #region Service and log check methods

        /// <summary>
        /// Pings the database, check will return an int if connected, otherwise returns an exception, 
        /// which is captured and passed as a string
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="connectionString"></param>
        /// <returns>string</returns>
        public static string CreateCommand(string queryString, string connectionString)
        {
            string isConn = "not connected";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Connection.Open();
                    var result = command.ExecuteNonQuery();
                    if (result.GetType().Equals(typeof(int)))
                    {
                        isConn = "connected";
                    }
                    else if (result.GetType().Equals(typeof(Exception)))
                    {
                        isConn = result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                isConn = ex.ToString();
            }

            return isConn;
        }

        /// <summary>
        /// Checks each connection in turn and returns a List of objects containing information about the connections.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>List of connection check objects</returns>
        public static List<ConnectionCheckResult> CheckConnections(Logger logger)
        {
            logger.Information("Checking connections.");
            List<ConnectionCheckResult> result = new List<ConnectionCheckResult>();

            //todo update these
            var connections = new List<ConnectionStringSettings>
            {
                ConfigurationManager.ConnectionStrings["X"],
                ConfigurationManager.ConnectionStrings["X"],
                ConfigurationManager.ConnectionStrings["X"],
            };
            var queryStrings = new List<string>
            {
                "SELECT [id], [x] FROM [d].[X]",
                "SELECT [id], [x] FROM [d].[X]",
                "SELECT [id], [x] FROM [d].[X]"
            };

            var infoToCheck = CreateTuples(connections, queryStrings, logger);

            foreach (var pair in infoToCheck)
            {
                try
                {
                    var connectionString = pair.Item1;
                    var queryString = pair.Item2;
                    var dbName = pair.Item1.Name;
                    var isConn = CreateCommand(queryString, connectionString.ToString());
                    if (isConn == "connected")
                    {
                        var connectResult = new ConnectionCheckResult
                        {
                            Message = "No errors returned. Details follow.",
                            DbName = dbName,
                            IsConnected = true,
                            LastCheckedOn = DateTime.Now
                        };
                        result.Add(connectResult);
                        logger.Information($"{dbName} is connected.");
                    }
                    else if (isConn != "connected")
                    {
                        var connectResult = new ConnectionCheckResult
                        {
                            Message = "An error was returned: ",
                            DbName = dbName,
                            IsConnected = false,
                            LastCheckedOn = DateTime.Now,
                            Errors = isConn
                        };
                        result.Add(connectResult);
                        logger.Warning($"{dbName} is not connected. See Details: {isConn}");
                    }
                }
                catch (System.Exception ex)
                {
                    logger.Error($"There was an error when attempting to check the connection: {ex}");
                }
            }

            logger.Information("Done checking connections, moving on to next step.");
            return result;
        }

        /// <summary>
        /// Creates a tuple containing each connection string and its query string
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="queryStrings"></param>
        /// <param name="logger"></param>
        /// <returns>List of tuples</returns>
        public static List<Tuple<ConnectionStringSettings, String>> CreateTuples(List<ConnectionStringSettings> connections, 
            List<string> queryStrings, Logger logger)
        {
            logger.Information("Matching connection and query strings into tuples.");

            var infoToCheck = new List<Tuple<ConnectionStringSettings, String>>();

            try
            {
                if(connections.Count == queryStrings.Count)
                {
                    for (int i = 0; i < connections.Count; i++)
                    {
                        Tuple<ConnectionStringSettings, string> temp = (connections[i], queryStrings[i]).ToTuple();
                        infoToCheck.Add(temp);
                    }
                    logger.Information("Completed tuple creation.");
                }
                else
                {
                    logger.Warning("Did not receive a matching number of connections and query strings. Process stopped.");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"There was an error when attempting to match the connection and query strings: {ex}");
            }

            return infoToCheck;
        }

        /// <summary>
        /// Pings the API server to verify that it is running and available.
        /// Reference: https://social.technet.microsoft.com/wiki/contents/articles/32243.c-how-to-check-whether-api-server-is-up-or-down.aspx
        /// </summary>
        /// <param name="logger"></param>
        public static List<ApiConnectionCheckResult> ServerStatusPing(Logger logger)
        {
            logger.Information("Starting to check API connections.");
            List<ApiConnectionCheckResult> checkResults = new List<ApiConnectionCheckResult>();
            //todo update here
            var connections = new List<string>
            {
                ConfigurationManager.AppSettings["X"],
                ConfigurationManager.AppSettings["X"], 
            };

            Ping checkApiServer = new Ping();

            try
            {
                foreach (var item in connections)
                {
                    PingReply serverResponse = checkApiServer.Send(item.ToString());

                    logger.Information("Checking API connection {0}", item.ToString());

                    if (serverResponse.Status == IPStatus.Success)
                    {
                        logger.Information("IP Address: {0}", serverResponse.Address.ToString());
                        logger.Information("RoundTrip time: {0}", serverResponse.RoundtripTime);
                        logger.Information("Time to live: {0}", serverResponse.Options.Ttl);
                        logger.Information("Don't fragment: {0}", serverResponse.Options.DontFragment);
                        logger.Information("Buffer size: {0}", serverResponse.Buffer.Length);

                        ApiConnectionCheckResult temp = new ApiConnectionCheckResult
                        {
                            Message = "No errors returned. Details follow.",
                            IpAddress = serverResponse.Address.ToString(),
                            RoundTripTime = serverResponse.RoundtripTime,
                            TimeToLive = serverResponse.Options.Ttl,
                            IsFragmented = serverResponse.Options.DontFragment,
                            BufferSize = serverResponse.Buffer.Length,
                            CheckedOn = DateTime.Now
                        };
                        checkResults.Add(temp);
                        logger.Information($"{serverResponse.Address} is connected.");
                    }
                    else
                    {
                        ApiConnectionCheckResult temp = new ApiConnectionCheckResult
                        {
                            Message = "An error was returned: ",
                            IpAddress = serverResponse.Address.ToString(),
                            RoundTripTime = serverResponse.RoundtripTime,
                            TimeToLive = serverResponse.Options.Ttl,
                            IsFragmented = serverResponse.Options.DontFragment,
                            BufferSize = serverResponse.Buffer.Length,
                            Error = serverResponse.Status.ToString(),
                            CheckedOn = DateTime.Now
                        };
                        checkResults.Add(temp);
                        logger.Warning($"{serverResponse.Address} is not connected. See Details: \n {serverResponse.Status.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"There was an error when attempting to check the connection: {ex}");
            }
            logger.Information("Done checking API connections, moving on to next step.");
            return checkResults;
        }

        /// <summary>
        /// Goes out and pulls all the log information for the past 24 hours.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>List of log objects</returns>
        public static List<List<string>> GetRecentLogs(Logger logger) 
        {
            logger.Information("Checking for new logs.");
            
            //todo update
            var connections = new List<ConnectionStringSettings>
            {
                ConfigurationManager.ConnectionStrings["X"], 
                ConfigurationManager.ConnectionStrings["X"], 
                ConfigurationManager.ConnectionStrings["X"], 
            };
            var queryStrings = new List<string>
            {
                "SELECT TOP 10 [Id], [X], [X], [X] FROM [d].[X] ORDER BY [X] DESC",
                "SELECT TOP 10 [Id], [X], [X], [X] FROM [d].[X] ORDER BY [X] DESC",  
                "SELECT TOP 10 [Id], [X], [X] FROM [d].[X] ORDER BY [X] DESC",
            };

            var infoToCheck = CreateTuples(connections, queryStrings, logger);

            List<List<string>> temp = new List<List<string>>();
            foreach (var pair in infoToCheck)
            {
                try
                {
                    var connectionString = pair.Item1;
                    var queryString = pair.Item2;

                    using (SqlConnection connection = new SqlConnection(connectionString.ToString()))
                    {
                        using (SqlCommand command = new SqlCommand(queryString, connection))
                        {
                            command.Connection.Open();
                            using (SqlDataReader response = command.ExecuteReader())
                            {
                                if (response.HasRows)
                                {
                                    while (response.Read())
                                    {
                                        var tempItem = new List<string>();

                                        while (response.Read())
                                        {
                                            for (int y = 0; y < response.FieldCount; y++)
                                            {
                                                var theCase = response.GetDataTypeName(y);

                                                switch (theCase)
                                                {
                                                    case "int":
                                                        tempItem.Add(response.GetInt32(y).ToString());
                                                        break;
                                                    case "tinyint":
                                                        tempItem.Add(response.GetInt32(y).ToString());
                                                        break;
                                                    case "bigint":
                                                        tempItem.Add(response.GetInt32(y).ToString());
                                                        break;
                                                    case "varchar":
                                                        tempItem.Add(response.GetString(y));
                                                        break;
                                                    case "nvarchar":
                                                        tempItem.Add(response.GetString(y));
                                                        break;
                                                    case "datetime":
                                                        tempItem.Add(response.GetDateTime(y).ToString());
                                                        break;
                                                    case "datetime2":
                                                        tempItem.Add(response.GetDateTime(y).ToString());
                                                        break;
                                                    case "datetimeoffset":
                                                        tempItem.Add(response.GetDateTimeOffset(y).ToString());
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                        }
                                        temp.Add(tempItem);
                                        response.NextResult();
                                    }
                                }
                                response.Close();
                            }
                        }
                        connection.Close();
                    }
                    logger.Information("Log check was successful.");
                }
                catch (System.Exception ex)
                {
                    logger.Error($"There was an error when attempting to check for new logs: {ex}");
                }
            }

            logger.Information("Done checking logs, moving on to next step.");
            return temp;
        }

        /// <summary>
        /// Pulls the logged information from a file.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>Tuple of strings</returns>
        public static Tuple<string, string> GetLogsFromFiles(Logger logger)
        {
            logger.Information("Starting to pull information from text file logs.");
            string results = "";
            string fileName = "";

            try
            {
                var fileLogPath = ConfigurationManager.AppSettings["X"];
                DirectoryInfo info = new DirectoryInfo(fileLogPath);
                FileInfo fileInfo = info.GetFiles().OrderByDescending(a => a.CreationTime).Take(2).Skip(1).FirstOrDefault();
                fileName = fileInfo.FullName;
                results = File.ReadAllText(fileInfo.FullName);
                logger.Information("Done pulling information from text file logs.");
            }
            catch (Exception ex)
            {
                logger.Error($"There was an error while attempting to pull the logs from the file: {ex}");
            }

            logger.Information("Finished pulling logs from file, moving on to next step.");
            return Tuple.Create(results, fileName);
        }

        #endregion

        #region Email methods

        /// <summary>
        /// Takes the information from the previous methods and sends it in an email to update on various statuses.
        /// </summary>
        /// <param name="dbConnectionResults"></param>
        /// <param name="apiConnectionResults"></param>
        /// <param name="logCheckResults"></param>
        /// <param name="logFileInfo"></param>
        /// <param name="logger"></param>
        public static void SendDbAndApiUpdateEmails(List<ConnectionCheckResult> dbConnectionResults, List<ApiConnectionCheckResult> apiConnectionResults,
             Logger logger)
        {
            logger.Information("Starting email method.");
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                var to = ConfigurationManager.AppSettings["emailsTo"];
                var fromUser = ConfigurationManager.AppSettings["emailFrom"];
                var fromPw = ConfigurationManager.AppSettings["emailPw"];

                message.From = new MailAddress(fromUser);
                message.To.Add(new MailAddress(to));
                message.Subject = "Server Health Check Update";
                message.IsBodyHtml = true;
                var messageBody1 = CreateDbMessageBody(dbConnectionResults, logger);
                var messageBody2 = CreateApiMessageBody(apiConnectionResults, logger);
                message.Body = "<h3>Database Results:</h3>" + messageBody1 + "<hr /><br /><h3>API Results:</h3>" + messageBody2 + "<br /> <h3>Message Ends</h3>";

                smtp.Port = Int32.Parse(ConfigurationManager.AppSettings["port"]);
                smtp.Host = ConfigurationManager.AppSettings["host"];
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(fromUser, fromPw);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
                logger.Information("Email was delivered successfully.");
            }
            catch (System.Exception ex)
            {
                logger.Error($"An error was encountered when sending email: {ex}");
            }
            logger.Information("At end of email method.");
        }

        /// <summary>
        /// Takes the information from the previous methods and sends it in an email to update on various statuses.
        /// </summary>
        /// <param name="logCheckResults"></param>
        /// <param name="logFileInfo"></param>
        /// <param name="logger"></param>
        public static void SendLogUpdateEmails(List<List<string>> logCheckResults, Tuple<string, string> logFileInfo, Logger logger)  
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                var to = ConfigurationManager.AppSettings["emailsTo"];
                var fromUser = ConfigurationManager.AppSettings["emailFrom"];
                var fromPw = ConfigurationManager.AppSettings["emailPw"];

                message.From = new MailAddress(fromUser);
                message.To.Add(new MailAddress(to));
                message.Subject = "Server Health Check Update";
                message.IsBodyHtml = true;
                var messageBody3 = CreateLogMessageBody(logCheckResults, logger);
                if (messageBody3 == "")
                {
                    messageBody3 = "No logs available.";
                }
                var messageBody4 = CreateFileLogMessageBody(logFileInfo, logger);
                message.Body = "<hr /><br /><h3>Recent log entries:</h3><br />" + messageBody3 + "<hr /><br /><h3>Recent log file entries:</h3><br />" +
                    messageBody4 + "<hr /><br /> <h3>Message Ends</h3>";

                smtp.Port = Int32.Parse(ConfigurationManager.AppSettings["port"]);
                smtp.Host = ConfigurationManager.AppSettings["host"];
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(fromUser, fromPw);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
                logger.Information("Email was delivered successfully.");
            }
            catch (Exception ex)
            {
                logger.Error($"An error was encountered when sending log email: {ex}");
            }
            logger.Information("At end of log email method.");
        }

        /// <summary>
        /// Converts the List<ConnectionCheckResult> into a string so it can be appended to the message body.
        /// </summary>
        /// <param name="connectionResults"></param>
        /// <param name="logger"></param>
        /// <returns>string</returns>
        public static string CreateDbMessageBody(List<ConnectionCheckResult> connectionResults, Logger logger)
        {
            logger.Information("Creating DB Section of Email Body.");
            StringBuilder sb = new StringBuilder();

            foreach (var item in connectionResults)
            {
                if (item.Errors != null || item.Errors != "")
                {
                    sb.Append(item.Message);
                    sb.Append("<br />Currently the database: ");
                    sb.Append("<b>" + item.DbName + "</b>");
                    sb.Append(" is connected: ");
                    sb.Append("<em>" + item.IsConnected.ToString() + "</em>");
                    sb.Append("<br />");
                    sb.Append("This information is current as of ");
                    sb.Append("<em>" + item.LastCheckedOn.ToString() + "</em>");
                    sb.Append("<br />");
                    sb.Append("If any errors occurred, they will appear below:<br /><br />");
                    sb.Append(item.Errors);
                    sb.Append("<br />-----------------------------------------<br /><br />");
                }
                else
                {
                    sb.Append(item.Message);
                    sb.Append("<br />Currently the database: ");
                    sb.Append("<b>" + item.DbName + "</b>");
                    sb.Append(" is connected: ");
                    sb.Append("<em>" + item.IsConnected.ToString() + "</em>");
                    sb.Append("<br />");
                    sb.Append("This information is current as of ");
                    sb.Append("<em>" + item.LastCheckedOn.ToString() + "</em>");
                    sb.Append("<br />");
                    sb.Append("If any errors occurred, they will appear below:<br /><br />");
                    sb.Append(item.Errors);
                    sb.Append("<br />-----------------------------------------<br /><br />");
                }
            }
            logger.Information("Completing DB Section of Email Body.");
            return sb.ToString();
        }

        /// <summary>
        /// Converts the List<ApiConnectionCheckResult> into a string so it can be appended to the message body.
        /// </summary>
        /// <param name="connectionResults"></param>
        /// <param name="logger"></param>
        /// <returns>string</returns>
        public static string CreateApiMessageBody(List<ApiConnectionCheckResult> connectionResults, Logger logger)
        {
            logger.Information("Creating Api Section of Email Body.");
            StringBuilder sb = new StringBuilder();

            foreach (var item in connectionResults)
            {
                if (item.Error != null || item.Error != "")
                {
                    sb.Append(item.Message);
                    sb.Append("<br />Currently the API Server: ");
                    sb.Append("<b>" + item.IpAddress + "</b>");
                    sb.Append(" is connected. <br />Round trip time was: ");
                    sb.Append("<em>" + item.RoundTripTime.ToString() + "</em>");
                    sb.Append("<br />");
                    sb.Append("The time to live is: ");
                    sb.Append("<em>" + item.TimeToLive.ToString() + "</em>");
                    sb.Append("<br />Don't fragment is: ");
                    sb.Append("<em>" + item.IsFragmented.ToString() + "</em>");
                    sb.Append("<br />The buffer size at the time of the check was: ");
                    sb.Append("<em>" + item.BufferSize.ToString() + "</em>");
                    sb.Append("<br />This information is current as of ");
                    sb.Append("<em>" + item.CheckedOn.ToString() + "</em>");
                    sb.Append("<br /><br />");
                    sb.Append("If any errors occurred, they will appear below:<br /><br />");
                    sb.Append(item.Error);
                    sb.Append("<br />-----------------------------------------<br /><br />");
                }
                else
                {
                    sb.Append(item.Message);
                    sb.Append("<br />Currently the API Server: ");
                    sb.Append("<b>" + item.IpAddress + "</b>");
                    sb.Append(" is not connected. <br />Round trip time was: ");
                    sb.Append("<em>" + item.RoundTripTime.ToString() + "</em>");
                    sb.Append("<br />");
                    sb.Append("The time to live is: ");
                    sb.Append("<em>" + item.TimeToLive.ToString() + "</em>");
                    sb.Append("<br />Don't fragment is: ");
                    sb.Append("<em>" + item.IsFragmented.ToString() + "</em>");
                    sb.Append("<br />The buffer size at the time of the check was: ");
                    sb.Append("<em>" + item.BufferSize.ToString() + "</em>");
                    sb.Append("<br />");
                    sb.Append("This information is current as of ");
                    sb.Append("<em>" + item.CheckedOn.ToString() + "</em>");
                    sb.Append("<br /><br />");
                    sb.Append("If any errors occurred, they will appear below:<br /><br />");
                    sb.Append(item.Error);
                    sb.Append("<br />-----------------------------------------<br /><br />");
                }
            }
            logger.Information("Completing Api Section of Email Body.");
            return sb.ToString();
        }

        /// <summary>
        /// Converts the List of log entry objects into a string so it can be appended to the email message
        /// </summary>
        /// <param name="logCheckResults"></param>
        /// <param name="logger"></param>
        /// <returns>string</returns>
        public static string CreateLogMessageBody(List<List<string>> logCheckResults, Logger logger)
        {
            logger.Information("Creating Log Section of Email Body.");
            StringBuilder sb = new StringBuilder();
            // todo update on change
            string[] logArray = { "X", "X", "X" };

            for (var item = 0; item < logCheckResults.Count; item++)
            {
                sb.Append($"\n*********<h4>Log Start: {logArray[item]}</h4>*********\n<br />");
                foreach (var thing in logCheckResults[item])
                {
                    int a;
                    if(int.TryParse(thing, out a))
                    {
                        sb.Append("<br /><br />");
                        sb.Append(thing);
                        sb.Append("<br />");
                    } 
                    else
                    {
                        sb.Append(thing);
                        sb.Append("<br />");
                    }
                }
            }

            logger.Information("Completing Logging Section of Email Body.");
            return sb.ToString();
        }

        /// <summary>
        /// Takes the Tuple of log info and the file name and formats them into something that is easier to read.
        /// </summary>
        /// <param name="logInfo"></param>
        /// <param name="logger"></param>
        /// <returns>string</returns>
        public static string CreateFileLogMessageBody(Tuple<string, string> logInfo, Logger logger)
        {
            logger.Information("Creating Log File Section of Email Body.");
            StringBuilder sb = new StringBuilder();

            var results = logInfo.Item1.Split("\r\n", StringSplitOptions.None).ToList();

            sb.Append("From file: ");
            sb.Append(logInfo.Item2);
            sb.Append("<br /><br />");

            foreach (var line in results)
            {
                sb.Append(line);
                sb.Append("\n<br />");
            }

            logger.Information("Completing Log File Section of Email Body");
            return sb.ToString();
        }

        #endregion Email methods
    }
}
