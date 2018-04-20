using iisTracing2syslog.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace iisTracing2syslogTest.Utils
{
    [TestClass]
    public class MessageFormatterTest
    {
        MessageFormatter formatter = new MessageFormatter();

        [TestMethod]
        public void ToObject()
        {
            // Work dir is <project root>/bin/<build.config>/
            var result = formatter.ToObject("../../src/iisTracing2syslogTest/test-data/fr000100.xml");

            Assert.IsNotNull(result);
            Assert.AreEqual("fr000100.xml", result["traceFile"]);

            Assert.AreEqual("http://localhost:80/err", result["url"]);
            Assert.AreEqual("1", result["siteId"]);
            Assert.AreEqual("GET", result["verb"]);
            Assert.AreEqual("", result["userName"]);
            Assert.AreEqual("STATUS_CODE", result["failureReason"]);
            Assert.AreEqual("404", result["statusCode"]);
            Assert.AreEqual("404", result["triggerStatusCode"]);
        }

        [TestMethod]
        public void ToMessage()
        {
            var result = formatter.ToMessage("../../src/iisTracing2syslogTest/test-data/fr000100.xml");
            Assert.IsTrue(result.Contains("\"traceFile\":\"fr000100.xml\""));
            Assert.IsTrue(result.Contains("\"url\":\"http://localhost:80/err\""));
        }
    }
}
