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
    public partial class Service1 : ServiceBase
    {
        private readonly string IIS_TRACING_PATTERN = "*.xml";

        private FileSystemWatcher _watcher;
        private SyslogClient _client;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // If a directory is not specified, exit program.
            if (args.Length != 2)
            {
                base.ExitCode = 2;
                base.Stop();
                return;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(args[1]);
            if (!directoryInfo.Exists)
            {
                base.ExitCode = 3;
                base.Stop();
                return;
            }

            _client = new SyslogClient("127.0.0.1", 5140, "UDP", false, null);

            StartMonitoring(directoryInfo);
        }

        public void StartMonitoring(DirectoryInfo directoryInfo)
        {          
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
        }


        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        }

        protected override void OnStop()
        {
            _client.Disconnect();
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
    }
}
