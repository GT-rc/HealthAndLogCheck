# Documentation for HealthAndLogCheck Application
### Lower Environment Health Check for Servers

*Overview:*
The purpose of this program is to ping various servers and verify that they are running. It will then format that information and send it out as an email to selected recipients. It can be expanded to return additional information, and was written to be reusable.

*Service and Log Methods:*
Create Command
- Parameters:
 1. String - query string - A generic query to run through the database. Columns and table names must be correct.
 2. String - connection string - The database connection string. Recommended best practice is to configure via .config or .json file. This pulls in the connection string for each database in turn.
- Function: This method pings the given database, using the given query string, to execute a non-query, then checks what was returned to verify if the database is currently running. If an exception is returned, it is captured and passed into the email and log.
- Returns a string containing either an integer (is connected) or an exception.

Check Connections
- Parameters:
 1. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: This method checks each connection string in turn, passing them into the Create Command method to verify that the database is connected. 
- Returns a list of CheckConnectionResult objects, each containing details about what was returned from each connection string. See Model section for object details.

Create Tuples
- Parameters:
 1. List<ConnectionStringSettings> connections - The database or app server connection string.
 2. List<String> query string - A query to run through the database.
 3. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: This method takes the connection strings and their corresponding query strings (which must be *in matching order* with the connection strings), and combines them into a tuple to be used by Check Connections.
- Returns a List<Tuple<ConnectionStringSettings, String>> of the matched connection and query strings to be broken apart in Check Connections or Get Recent Logs.

Get Recent Logs
- Parameters:
 1. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: To retrieve a list of objects, each containing one row from the log file in question, truncated to rows that have been added within the last day, as this will be set up to run once a day. Therefore, this *should* give a daily update email that will cover all log updates.
- Returns a List of LogCheckResult objects, each containing details about what was returned from the query.

Get Logs From Files
- Parameters: 
 1. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: To retrieve the information from a log file saved as a text file rather than to a database. Does require the base file path added to the config file. Orders by the date the file was created (descending), and pulls the file before the most recent - assumes that the most recent file is the one currently in use. May require additional tweaking depending on how logs are set up.
- Returns a Tuple<String, String> containing the log file in the first position and the name of the file it pulled from in the second position.

*API Methods*
Server Status Ping
- Parameters:
 1. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: This method pings the API server to verify that it is running and available.
- Returns a List<ApiConnectionCheckResult> of details returned by this check so it can be formatted into the email.

*Email Methods*
Send Update Emails
- Parameters:
 1. List<CheckConnectionResult> connection result - A collection of CheckConnectionResult objects containing details about each connection.
 2. List<ApiConnectionCheckResult> api connection results - A collection of ApiConnectioCheckResult objects containing details about each API server.
 3. List<LogCheckResult> log check results - A collection of LogCheckResult objects containing details about each line of logs that has been added within the last 24 hours.
 4. Tuple<String, String> log file info - Item 1 contains all the information from the log file, and item 2 contains the file name for reference.
 5. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: This method calls all the Create Message Body methods and takes that information as well as other email related data, concatenates it, and sends out an html email to the specified addresses. Specific information in this section is in the App Settings section of the config file. Values will need to be updated there.
- Returns nothing, just sends the email.

Create DB Message Body
- Parameters: 
 1. List<CheckConnectionResult> connection result - A collection of CheckConnectionResult objects containing details about each connection.
 2. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: This method uses String Builder to concatenate all the relevant information from the objects into a single string that can be put into the email.
- Returns a string containing the information for the body of the email.

Create API Message Body
- Parameters: 
 1. List<ApiConnectionCheckResult> connection result - A collection of ApiConnectionCheckResult objects containing details about each connection.
 2. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: This method uses String Builder to concatenate all the relevant information from the objects into a single string that can be put into the email.
- Returns a string containing the information for the body of the email.

Create Log Message Body
- Parameters: 
 1. List<LogCheckResult> log check results - A collection of LogCheckResult objects containing details about each line of logs that has been added within the last 24 hours.
 2. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: This method uses String Builder to concatenate all the relevant information from the objects into a single string that can be put into the email.
- Returns: Returns a string containing the information for the body of the email.

Create File Log Message Body
- Parameters: 
 1. Tuple<String, String> - log info - Item 1 contains all the information from the log file, and item 2 contains the file name for reference.
 2. Logger - logger - The logger from the main method so that logging information will all be recorded together.
- Function: This method splits the original log file on the CR LF line endings, and then uses String Builder and a foreach loop to concatenate all the information into an easier to read string that can be added to the email.
- Returns: Returns a string containing the information for the body of the email.

*Main Method*
Main
- No Parameters
- Function: Instantiates logging to be passed through all the methods. Calls Check Connections, Server Status Ping, Get Recent Logs, and Get Logs From Files to gather all the needed information; then calls Send Update Emails, and logs any relevant information about the process.
- Returns nothing, simply the entry point into the application.

*Logging:*
Logging is set up using Serilog, and settings are currently in the app.config file. As of now the logs are written to the Console, a File, and a MSSQL database table.
- Console Details: None
- File Details: Currently located in [C:\Temp\Logs\HealthAndLogCheck_Console], each log file is appended with the date it was written, and is set to roll into a new file each day, or when it exceeds the set file size limit of 1GB. This can be changed with the parameter **fileSizeLimitBytes: null**. Currently using the default for number of files retained, which is 31 days. This can be changed with the parameter **retainedFileCountLimit: null**.
- Database Details: Currently this is writing to [AnotherTest.dbo.Logs] in my test database, and is set to auto-generate a table named Logs in the table it's pointed to, so this should be simple to update.

*Models:*
Check Connection Result
- Properties: 
 1. String Message - holds any message passed about the connection.
 2. String DbName - holds the name of the relevant database.
 3. Boolean IsConnected - T/F value for if database is connected.
 4. DateTime LastCheckedOn - set at time of database check, holds the timestamp for when the connection was checked.
 5. String Errors - holds any error messages or exceptions.

 Api Connection Check Result
 - Properties:
  1. String Message - holds any message passed about the connection.
  2. String IpAddress - holds the IP Address for the server being checked.
  3. Long RoundTripTime - holds the number of milliseconds taken to send the Internet Control Message Protocol (ICMP) echo request and get the reply.
  4. Int TimeToLive - by default should be 128.
  5. Boolean IsFragmented - holds the T/F value for if there is fragmentation. There is an option to set this to false.
  6. Int BufferSize - holds the buffer size received in the echo reply.
  7. String Error - holds any error messages or exceptions.
  8. DateTime CheckedOn - set at time of database check, holds the timestamp for when the connection was checked.

Log Check Result
- Properties:
 1. Int LogId - holds the ID (primary key) for the log row in the log table.
 2. String Message - holds the main message generated by the program and saved to the row.
 3. String MessageTemplate - holds the log event message with property placeholders (similar to message, so not included in email currently).
 4. Sting Level - holds the level of the error message (i.e. Information/Warning/Error/etcetera).
 5. DateTime TimeStamp - holds the timestamp for when the error row was generated.
 6. String Exception - holds any exceptions that were generated and saved that are not part of the message.
 7. String Properties - can hold a variety of properties, and there are options to customize this column, but currently it is not being used.
 8. String AdditionalMessage - this is a placeholder for any additional information that someone would want to pass along, currently it is not being used.

*Other Notes*
I have moved most of the comments out of the code and into this documentation, with a few exceptions. 
I have replaced all real data with 'X's to mask non-public information.

Options for next steps: 
- Add JS to make sections collapsible
- Separate into two different emails
- Add environment information
- Set up to run scheduled at 7am each day
