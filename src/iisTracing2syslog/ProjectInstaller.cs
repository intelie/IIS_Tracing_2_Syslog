using iisTracing2syslog.Utils;
using System.ComponentModel;
using System.Configuration.Install;

namespace iisTracing2syslog
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            if (!System.Diagnostics.EventLog.SourceExists(IISTracing2Syslog.EVENT_SOURCE))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    IISTracing2Syslog.EVENT_SOURCE, IISTracing2Syslog.EVENT_LOG);
            }

            Configuration config = new Configuration();
            var currentConfigValue = config.SyslogHost;
            if (currentConfigValue == null || currentConfigValue == "")
            {
                config.WriteDefaultConfiguration();
            }
        }
    }
}
