using Microsoft.Win32;
using System;

namespace iisTracing2syslog.Utils
{
    public class Configuration
    {
        public static readonly string DEFAULT_SERVER = "mysyslogserver.example.net";
        public static readonly long DEFAULT_PORT = 514;
        public static readonly string DEFAULT_LOG_PATH = @"C:\inetpub\logs\FailedReqLogFiles\W3SVC1";

        private static readonly string BASE_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Intelie\IISTracing2Syslog";

        public string SyslogHost
        {
            get
            {
                return GetString("Network", "Destination");
            }
            set {
                SetString("Network", "Destination", value);
            }
        }

        public long SyslogPort
        {
            get
            {
                return GetDword("Network", "DestPort");
            }
            set
            {
                SetDword("Network", "DestPort", value);
            }
        }

        public string LogPath
        {
            get
            {
                return GetString("Log", "Path");
            }
            set
            {
                SetString("Log", "Path", value);
            }
        }

        public void WriteDefaultConfiguration()
        {
            SyslogHost = DEFAULT_SERVER;
            SyslogPort = DEFAULT_PORT;
            LogPath = DEFAULT_LOG_PATH;
        }

        private string GetString(string path, string key)
        {
            return (string)Registry.GetValue(BASE_KEY + @"\" + path, key, "");
        }

        private long GetDword(string path, string key)
        {
            var value = Registry.GetValue(BASE_KEY + @"\" + path, key, 0L);
            if (value == null) return 0L;

            return Convert.ToInt64(value);
        }

        private void SetString(string path, string key, string value)
        {
            Registry.SetValue(BASE_KEY + @"\" + path, key, value, RegistryValueKind.String);
        }

        private void SetDword(string path, string key, long value)
        {
            Registry.SetValue(BASE_KEY + @"\" + path, key, value, RegistryValueKind.DWord);
        }
    }
}
