using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Web.Script.Serialization;

namespace iisTracing2syslog
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
            var fileInfo = new FileInfo(tracingXmlPath);

            var result = new Dictionary<string, string>();
            result.Add("traceFile", fileInfo.Name);

            XmlDocument doc = new XmlDocument();
            doc.Load(fileInfo.FullName);
            var failedRequestNode = doc.SelectSingleNode("/failedRequest");
            foreach(string attributeName in IMPORTANT_ATTRIBUTES)
            {
                string attrValue = failedRequestNode.Attributes[attributeName]?.InnerText;
                if (attrValue != null)
                    result.Add(attributeName, attrValue);
            }

            return result;
        }
    }
}
