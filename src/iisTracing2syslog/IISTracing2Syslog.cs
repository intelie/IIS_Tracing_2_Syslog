using iisTracing2syslog.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace iisTracing2syslog
{
    public partial class IISTracing2Syslog : ServiceBase
    {
        private static readonly string IIS_TRACING_PATTERN = "*.xml";

        public static readonly string EVENT_SOURCE = "Tracing2Syslog";
        public static readonly string EVENT_LOG = "Intelie";

        private FileSystemWatcher _watcher;
        private SyslogClient _client;
        private MessageFormatter _formatter = new MessageFormatter();

        public IISTracing2Syslog()
        {
            InitializeComponent();

            eventLog.Source = EVENT_SOURCE;
            eventLog.Log = EVENT_LOG;
        }
        
        protected override void OnStart(string[] args)
        {
            try
            {
                Configuration config = new Configuration();

                var syslogHost = config.SyslogHost;
                var syslogPort = config.SyslogPort;
                var logPath = config.LogPath;

                if (syslogHost == null || syslogHost == "") throw new ArgumentException("Syslog server is not configured");
                if (syslogPort == 0) throw new ArgumentException("Syslog server port is not configured");
                if (logPath == null || logPath == "") throw new ArgumentException("Monitored log path is not configured");

                DirectoryInfo directoryInfo = new DirectoryInfo(logPath);

                if (!directoryInfo.Exists) throw new ArgumentException("Monitored log path does not exist");

                _client = new SyslogClient(syslogHost, 5140, "UDP", false, null);
                _client.AppName = "IISFailedRequest";
                _client.PrependMessageLength = false;
                _client.DiagnosticsEventLog = eventLog;

                // Create a new FileSystemWatcher and set its properties.
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = directoryInfo.FullName;

                //watcher.NotifyFilter = NotifyFilters.LastWrite;

                watcher.Filter = IIS_TRACING_PATTERN;

                // Add event handlers.
                watcher.Created += new FileSystemEventHandler(OnChanged);

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                _watcher = watcher;

                _client.Activate();

                eventLog.WriteEntry("Service started. Monitoring " + logPath);
            }
            catch (ArgumentException ae)
            {
                eventLog.WriteEntry("An error occured during the service startup: " + ae.Message, EventLogEntryType.Error);

                if (System.Environment.UserInteractive) throw ae;

                base.ExitCode = 87; // ERROR_INVALID_PARAMETER
                base.Stop();
            } catch (Exception e)
            {
                eventLog.WriteEntry("An error occured during the service startup: " + e.ToString(), EventLogEntryType.Error);

                if (System.Environment.UserInteractive) throw e;

                base.ExitCode = 352; // ERROR_FAIL_RESTART
                base.Stop();
            }
        }
        
        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                string msg = _formatter.ToMessage(e.FullPath);
                _client.SendTracingEvent(msg);
            } catch (Exception ex)
            {
                if (System.Environment.UserInteractive)
                    Console.Error.WriteLine("Error sending event for tracing file [" + e.Name + "]. " + ex.ToString());

                eventLog.WriteEntry("Error sending event for tracing file [" + e.Name + "]. " + ex.ToString(), EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("Stopping service");
            if (_client != null)
            {
                _client.Disconnect();
                _client.Dispose();
            }            
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }            
        }


        // Alternative entry point to test as console application
        public void StartStandalone()
        {
            OnStart(null);
        }

        public void StopStandalone()
        {
            OnStop();
        }
    }
}
