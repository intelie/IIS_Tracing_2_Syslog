using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml;

namespace iisTracing2syslog.Utils
{
    public class MessageFormatter
    {
        private static readonly string[] IMPORTANT_ATTRIBUTES = {
            "url","siteId","verb","userName","failureReason","statusCode","triggerStatusCode" };

        JavaScriptSerializer json = new JavaScriptSerializer();

        public string ToMessage(string tracingXmlPath)
        {
            var obj = ToObject(tracingXmlPath);
            return json.Serialize(obj);
        }

        public Dictionary<string, string> ToObject(string tracingXmlPath)
        {
            string xmlContents = tryReadContents(tracingXmlPath);

            var result = new Dictionary<string, string>();

            result.Add("traceFile", new FileInfo(tracingXmlPath).Name);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContents);
            var failedRequestNode = doc.SelectSingleNode("/failedRequest");
            foreach(string attributeName in IMPORTANT_ATTRIBUTES)
            {
                string attrValue = failedRequestNode.Attributes[attributeName]?.InnerText;
                if (attrValue != null)
                    result.Add(attributeName, attrValue);
            }

            return result;
        }

        private string tryReadContents(string tracingXmlPath)
        {
            var retryCount = 0;
            while(retryCount < 2)
            {
                retryCount++;
                try
                {
                    return File.ReadAllText(tracingXmlPath);
                } catch (IOException ioe)
                {
                    // At random times, the watcher is notified before the system finishes writing the file
                    // (IOException: the file is used by another process)
                    // We sleep a little and retry once before allowing the operation to fail
                    if (retryCount > 1) throw ioe;

                    Thread.Sleep(10);
                }
            }
            // Should never reach this point!
            throw new InvalidOperationException("Error in retry procedure");
        }
    }
}
