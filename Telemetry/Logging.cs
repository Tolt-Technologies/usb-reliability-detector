using NLog;
using System;
using System.Diagnostics;
using System.IO;

namespace ToltTech.Telemetry
{
    internal static class Logging
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string NLOG_LAYOUT = $"{AppInfo.Version}|${{level}}|${{logger}}|${{message:withexception=true}}";

        private static readonly string LogFileName = AppInfo.Name + ".Log";
        private static string _LogFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppInfo.Name, LogFileName);
        public static string LogFilePath
        {
            get
            {
                return _LogFilePath;
            }
        }

        public static void Initialize(bool debugMode, string additionalSessionInformation = null)
        {
            Logger.Trace("Initialize");

            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget()
            {
                FileName = LogFilePath,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 5,
                KeepFileOpen = true,
                OpenFileCacheTimeout = 4 * 60 * 60
            };

            var debugOutput = new NLog.Targets.OutputDebugStringTarget
            {
                Layout = $"{AppInfo.Name}|{NLOG_LAYOUT}"
            };

            var debug = new NLog.Targets.DebuggerTarget
            {
                Layout = $"${{date:format=ss.fff}}|{AppInfo.Name}|{NLOG_LAYOUT}"
            };

            if (debugMode)
            {
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, debug);
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, debugOutput);

                if (!Debugger.IsAttached) // Ergo, Debug is set in Settings file
                {
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
                }
            }
            else
            {
                // These allow us to run a local app like DebugView on a customer system and see
                // detailed telemetry without reconfiguring the app.
                // Also allows us to start DebugView on a system functioning strangely without needing
                // to restart the app.

                config.AddRule(LogLevel.Debug, LogLevel.Fatal, debug);
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, debugOutput);

                config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            }

            LogManager.Configuration = config;
        }
    }
}
