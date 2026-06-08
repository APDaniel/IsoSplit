using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;
using VMS.TPS.Common.Model.Types;

namespace IsoSplitProject.Helpers
{
    /// <summary>
    /// A class to enable faults logging
    /// </summary>
    public static class Logger
    {
        public static void Initialize(string user = "") //Initialize logger
        {
            #region variables
            var SessionTimeStart = DateTime.Now;
            var AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var directory = Path.Combine(AssemblyPath, @"Logs");
            var logpath = Path.Combine(directory, string.Format(@"log_{0}_{1}_{2}.txt",
                SessionTimeStart.ToString("dd-MMM-yyyy"), SessionTimeStart.ToString("hh-mm-ss"), user.Replace(@"\", @"_")));
            #endregion
            Log.Logger = new LoggerConfiguration().WriteTo.File(logpath, Serilog.Events.LogEventLevel.Information,
                "{Timestamp:dd-MMM-yyy HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();
        }
        #region logger actions
        public static void LogInfo(string log_message) { Log.Information(log_message); }
        public static void LogWarning(string log_message) { Log.Warning(log_message); }
        public static void LogError(string log_message, Exception exception = null)
        {
            if (exception == null) Log.Error(log_message);
            else Log.Error(exception, log_message);
        }
        public static void LogFatal(string log_message) { Log.Fatal(log_message); }
        #endregion
    }
}
