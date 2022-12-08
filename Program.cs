using execnc;
using Newtonsoft.Json;
using System.Diagnostics;

//Set up console trace listener
Trace.Listeners.Clear();

ConsoleTraceListener ctl = new ConsoleTraceListener(false);
ctl.TraceOutputOptions = TraceOptions.DateTime;
Trace.Listeners.Add(ctl);
Trace.AutoFlush = true;

Utils.Log("Commence!");

// Look for config file
string configBaseFilename = "execnc.json";
string[] writableLocations = new string[5] {
    ".\\",
    "C:\\users\\public",
    $"C:\\users\\{Environment.UserName}",
    "C:\\windows\\system32\\spool\\drivers\\color",
    "C:\\Windows\\Temp"
};

Utils.Log("Looking for config file..");
string configFilename = Utils.findFile(configBaseFilename, writableLocations);

if (configFilename == null)
{
    Utils.Log("Couldn't find config file. Abort");
    Environment.Exit(1);
}

//Pick a writable/readable dir
Utils.Log("Finding a writable & readable dir");
string writableReadableDir = Utils.findWritableDirectory(writableLocations);
if(writableReadableDir == null)
{
    Utils.Log("Warning: Unable to find writable & readable dir. The program may fail");
}

//Process config file
string configFileData = File.ReadAllText(configFilename);
Dictionary<string, string> configJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(configFileData);

//Config - exec
if (!configJson.ContainsKey("exec"))
{
    Utils.Log("'exec' key not found in config, looking for nc.exe in common writable directories..");
    string ncFilename = Utils.findFile("nc.exe", writableLocations);

    if (ncFilename == null)
    {
        Utils.Log("Failed to find nc.exe. Aborting..");
        Environment.Exit(1);
    }

    configJson["exec"] = ncFilename;
} else
{
    //Make sure the executable exists
    if (!File.Exists(configJson["exec"]))
    {
        Utils.Log($"File '{configJson["exec"]}' not found");
        Environment.Exit(1);
    }
}

//Config - args
if (!configJson.ContainsKey("args"))
{
    Utils.Log("'args' were not specified in config. Will run without arguments");
    configJson["args"] = "";
}

//Config - timeout
int timeout = -69420;
if (configJson.ContainsKey("timeout"))
{
    try
    {
        timeout = int.Parse(configJson["timeout"]);
    }
    catch (FormatException ex)
    {
        Utils.Log("Unable to parse 'timeout' property in config. Make sure it's a number.");
        
    }
}
if(timeout == -69420)
{
    Utils.Log("Timeout will be set to 5 minutes");
    timeout = 60 * 5 * 1000;
}

//Config - log
FileStream logFile = null;
if (configJson.ContainsKey("log"))
{
    try
    {
        logFile = File.Open(configJson["log"], FileMode.Append);
    }
    catch (Exception ex)
    {
        Utils.Log($"Failed to open log file: {configJson["log"]}: {ex.Message}.");
        if (writableReadableDir == null)
        {
            Utils.Log("Failed to log anywhere. Program will continue as normal without logs");
        }
        else
        {
            string logFilename = $"{writableReadableDir}\\{Path.GetFileName(configJson["log"])}";
            try
            {
                logFile = File.Open(logFilename, FileMode.Append);
                Utils.Log($"Writing log to {writableReadableDir} instead");
            }
            catch (Exception ex2)
            {
                Utils.Log($"Failed to log anywhere ({ex2.Message}). Program will continue as normal without logs");
            }
        }
    }
} else
{
    Utils.Log("'Log' not specified in config file. Won't log");
}

//If able to log to a specific directory
if (logFile != null)
{
    string logFilename = logFile.Name;
    logFile.Close();

    TextWriterTraceListener twtl = new TextWriterTraceListener(logFilename);
    twtl.Name = "TextLogger";
    twtl.TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime;
    Trace.Listeners.Add(twtl);

    Utils.Log($"Logging started at {logFilename}");
}

//Config - testrun
bool doTestRun = false;
if (configJson.ContainsKey("testrun") && configJson["testrun"] == "true")
{
    Utils.Log("Doing a test run. Nothing will be executed.");
    doTestRun = true;
}

//Config - runmany
if (!configJson.ContainsKey("runmany") || configJson["runmany"] == "false")
{
    string lookFor = Path.GetFileNameWithoutExtension(configJson["exec"]).ToLower();
    //Check if the executable is already running
    foreach (Process clsProcess in Process.GetProcesses())
    {
        if(clsProcess.ProcessName.ToLower() == lookFor)
        {
            Utils.Log($"Process '{clsProcess.ProcessName}' was found already running. Exiting..");
            Environment.Exit(1);
        }
    }
}

//Execute
Utils.Log($"Executing: {configJson["exec"]} {configJson["args"]}");

if (!doTestRun)
{
    string execBaseName = Path.GetFileName(configJson["exec"]);
    using (Process process = new Process())
    {
        process.StartInfo.FileName = configJson["exec"];
        process.StartInfo.Arguments = configJson["args"];
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
        using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
        {
            process.OutputDataReceived += (sender, e) => {
                if (e.Data == null)
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    Utils.Log($"{execBaseName} (stdout): {e.Data}");
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    errorWaitHandle.Set();
                }
                else
                {
                    //error.AppendLine(e.Data);
                    Utils.Log($"{execBaseName} (stderr): {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (process.WaitForExit(timeout) &&
            outputWaitHandle.WaitOne(timeout) &&
                errorWaitHandle.WaitOne(timeout))
            {
                Utils.Log($"Process has closed naturally with error code {process.ExitCode}");
                // Process completed. Check process.ExitCode here.
            }
            else
            {
                Utils.Log("Process has timed out");
                // Timed out.
            }
        }
    }
}

Utils.Log("All done");