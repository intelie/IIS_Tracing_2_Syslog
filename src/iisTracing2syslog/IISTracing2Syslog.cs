using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
            eventLog.WriteEntry("Starting service");
            DirectoryInfo directoryInfo = new DirectoryInfo("C:\\inetpub\\logs\\FailedReqLogFiles\\W3SVC1");
            
            _client = new SyslogClient("lognit.intelie", 5140, "UDP", false, null);
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
            _client.Disconnect();
            _client.Dispose();
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
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
