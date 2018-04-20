using System;

namespace iisTracing2syslog.Utils
{
    internal class Iso8601DatePatternConverter
    {
        public const string Iso8601Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public static string FormatString(DateTime val)
        {
            return val.ToUniversalTime().ToString(Iso8601Format);
        }
    }
}