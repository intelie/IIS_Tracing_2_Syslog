﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;


namespace iisTracing2syslog
{
    internal class SyslogClient
    {
        public SyslogClient()
        {
            Hostname = Dns.GetHostName();
            Version = 1;
            Facility = "User";
            TrailerChar = '\n';
            Protocol = "TCP";
            SSL = true;

            //_sendingPeriod = _defaultSendingPeriod;
            //Fields = new Dictionary<string, string>();

            //_senderThread = new Thread(SenderThreadEntry)
            //{
            //    Name = "SenderThread",
            //    IsBackground = true,
            //};
        }

        public SyslogClient(string server, int port, string certificatePath)
            : this()
        {
            Server = server;
            Port = port;
            CertificatePath = certificatePath;
        }

        public SyslogClient(string server, int port, string protocol, bool ssl, string certificatePath)
            : this()
        {
            Server = server;
            Port = port;
            Protocol = protocol;
            SSL = ssl;
            CertificatePath = certificatePath;
        }

        public string Server { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; }
        public bool SSL { get; set; }
        public string CertificatePath { get; set; }
        public string Certificate { get; set; }

        public Dictionary<string, string> Fields { get; set; }

        public string Facility { get; set; }
        public int Version { get; private set; }

        public char? TrailerChar { get; set; }

        public int TrailerCharCode
        {
            get { return (TrailerChar != null) ? (int)TrailerChar : 0; }
            set { TrailerChar = (char)value; }
        }

        public string Hostname
        {
            get { return _hostname ?? "-"; }
            set { _hostname = value; }
        }

        private string _hostname;

        public string AppName
        {
            get { return _appName ?? "-"; }
            set { _appName = value; }
        }

        private string _appName;

        public string ProcId
        {
            get { return _procId ?? "-"; }
            set { _procId = value; }
        }

        private string _procId;

        public string MessageId
        {
            get { return _messageId ?? "-"; }
            set { _messageId = value; }
        }

        private string _messageId;

        // NOTE see https://tools.ietf.org/html/rfc5424#section-7.2.2
        public string StructuredDataId
        {
            get { return _structuredDataId ?? "fields"; }
            set { _structuredDataId = value; }
        }

        private string _structuredDataId;

        public string EnterpriseId
        {
            get { return _enterpriseId ?? "0"; }
            set { _enterpriseId = value; }
        }

        private string _enterpriseId = "0";

        public int MaxQueueSize = 1024 * 1024;

        public bool EnableRemoteDiagnosticInfo
        {
            get { return _enableRemoteDiagnosticInfo; }
            set { _enableRemoteDiagnosticInfo = value; }
        }

        private volatile bool _enableRemoteDiagnosticInfo = true;

        public void AddField(string text)
        {
            var parts = text.Split('=');
            if (parts.Count() != 2)
                throw new ArgumentException();

            var value = parts[1];
            if (value.StartsWith("$"))
            {
                value = value.Substring(1);
                value = Environment.GetEnvironmentVariable(value);
            }
            Fields.Add(parts[0], value);
        }

        public void SendTracingEvent(string file)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            try
            {
                var frame = FormatMessage(file);

                lock (_messageQueue)
                {
                    if (_messageQueue.Count == MaxQueueSize - 1)
                    {
                        var warningMessage = string.Format(
                            "Message queue size ({0}) is exceeded. Not sending new messages until the queue backlog has been sent.", MaxQueueSize);
                        //LogDiagnosticInfo(warningMessage);
                    }
                    if (_messageQueue.Count >= MaxQueueSize)
                        return;
                    _messageQueue.Enqueue(frame);
                }
            }
            catch (Exception exc)
            {
                LogError(exc);
            }
        }

        private string FormatMessage(string file, Dictionary<string, string> extraFields = null)
        {
            return "NEW FILE: " + file;

            //var sourceMessage = RenderLoggingEvent(val);
            //var structuredData = FormatStructuredData(extraFields);

            //var time = Iso8601DatePatternConverter.FormatString(DateTime.UtcNow);
            //var message = string.Format("<{0}>{1} {2} {3} {4} {5} {6} {7}{8}",
            //    GeneratePriority(val.Level), Version, time, Hostname, AppName, ProcId, MessageId, structuredData, sourceMessage);
            //var frame = string.Format("{0} {1}", message.Length, Escape(message));
            //if (TrailerChar != null)
            //    frame += TrailerChar;
            //return frame;
        }

        //private string FormatStructuredData(Dictionary<string, string> extraFields = null)
        //{
        //    var res = "";
        //    if (Fields.Count > 0 && !string.IsNullOrEmpty(EnterpriseId))
        //        res = FormatStructuredFields(Fields);

        //    if (extraFields != null && extraFields.Count > 0)
        //        res += " " + FormatStructuredFields(extraFields);

        //    if (!string.IsNullOrEmpty(res))
        //        return string.Format("[{0}@{1} {2}] ", StructuredDataId, EnterpriseId, res);

        //    return res;
        //}

        //private static string FormatStructuredFields(Dictionary<string, string> fields)
        //{
        //    return string.Join(" ", fields.Select(pair => string.Format("{0}=\"{1}\"", pair.Key, EscapeStructuredValue(pair.Value))));
        //}
        
        //int GeneratePriority(Level level)
        //{
        //    return RemoteSyslogAppender.GeneratePriority(Facility, GetSeverity(level));
        //}

        //public static SyslogSeverity GetSeverity(Level level)
        //{
        //    if (level >= Level.Alert)
        //        return SyslogSeverity.Alert;

        //    if (level >= Level.Critical)
        //        return SyslogSeverity.Critical;

        //    if (level >= Level.Error)
        //        return SyslogSeverity.Error;

        //    if (level >= Level.Warn)
        //        return SyslogSeverity.Warning;

        //    if (level >= Level.Notice)
        //        return SyslogSeverity.Notice;

        //    if (level >= Level.Info)
        //        return SyslogSeverity.Informational;

        //    return SyslogSeverity.Debug;
        //}

        //static string EscapeStructuredValue(string val)
        //{
        //    var buf = new StringBuilder(val);
        //    buf.Replace("\\", "\\\\");
        //    buf.Replace("\"", "\\\"");
        //    buf.Replace("]", "\\]");
        //    return buf.ToString();
        //}

        private void EnsureConnected()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            lock (_connectionSync)
            {
                if (_socket != null)
                    return;

                var endpoint = ResolveServerEndpoint();

                Socket socket;
                if (IsTcp())
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(endpoint);

                    var rawStream = new NetworkStream(socket);

                    if (SSL)
                    {
                        var sslStream = new SslStream(rawStream, false, VerifyServerCertificate);
                        var certificate = (string.IsNullOrEmpty(CertificatePath))
                            ? new X509Certificate(Encoding.ASCII.GetBytes(Certificate.Trim()))
                            : new X509Certificate(CertificatePath);
                        var certificates = new X509CertificateCollection(new[] { certificate });
                        sslStream.AuthenticateAsClient(Server, certificates, SslProtocols.Tls, false);

                        _stream = sslStream;
                    }
                    else
                    {
                        _stream = rawStream;
                    }

                    _writer = new StreamWriter(_stream, Encoding.UTF8);

                }
                else
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _writer = null;
                }

                _socket = socket;
                _remoteEndpoint = endpoint;
            }
        }

        private bool IsTcp()
        {
            return Protocol != null && Protocol.ToUpper() == "TCP";
        }

        private IPEndPoint ResolveServerEndpoint()
        {
            var addresses = System.Net.Dns.GetHostAddresses(Server);
            if (addresses.Length == 0)
            {
                throw new ArgumentException(
                    "Unable to retrieve address from specified host name.",
                    "hostName"
                );
            }

            var endpoint = new IPEndPoint(addresses[0], Port);
            return endpoint;
        }

        private static bool VerifyServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        //private void SenderThreadEntry()
        //{
        //    try
        //    {
        //        while (!_disposed)
        //        {
        //            if (_log.IsErrorEnabled)
        //            {
        //                LogStartupInfo();
        //                break;
        //            }
        //            Thread.Yield();
        //        }

        //        while (!_disposed)
        //        {
        //            TrySendMessages();
        //            if (_closing)
        //                break;

        //            var startTime = DateTime.UtcNow;
        //            while (DateTime.UtcNow - startTime < _sendingPeriod && !_closing)
        //                Thread.Sleep(10);
        //        }
        //    }
        //    catch (ThreadInterruptedException)
        //    {
        //    }
        //    catch (Exception exc)
        //    {
        //        LogError(exc);
        //    }
        //}

        //private void TrySendMessages()
        //{
        //    try
        //    {
        //        SendMessages();
        //    }
        //    catch (ThreadInterruptedException)
        //    {
        //    }
        //    catch (ThreadAbortException)
        //    {
        //    }
        //    catch (ObjectDisposedException)
        //    {
        //    }
        //    catch (Exception exc)
        //    {
        //        LogError(exc);
        //    }
        //}

        //void SendMessages()
        //{
        //    lock (_sendingSync)
        //    {
        //        Socket socket = null;

        //        try
        //        {
        //            EnsureConnected();

        //            TextWriter writer;
        //            lock (_connectionSync)
        //            {
        //                if (_socket == null)
        //                    return;
        //                socket = _socket;
        //                writer = _writer;
        //            }

        //            _sendingPeriod = _defaultSendingPeriod;

        //            while (true)
        //            {
        //                string frame;

        //                lock (_messageQueue)
        //                {
        //                    if (_messageQueue.Count == 0)
        //                        break;
        //                    frame = _messageQueue.Peek();
        //                }

        //                if (IsTcp())
        //                {
        //                    writer.Write(frame);
        //                    writer.Flush();
        //                }
        //                else
        //                {
        //                    byte[] sendBuffer = Encoding.UTF8.GetBytes(frame);
        //                    socket.SendTo(sendBuffer, _remoteEndpoint);
        //                }

        //                lock (_messageQueue)
        //                {
        //                    _messageQueue.Dequeue();
        //                }
        //            }

        //            return;
        //        }
        //        catch (SocketException exc)
        //        {
        //            if (!IgnoreSocketErrors.Contains(exc.SocketErrorCode))
        //                LogError(exc);
        //        }
        //        catch (IOException exc)
        //        {
        //            if ((uint)exc.HResult != 0x80131620) // COR_E_IO
        //                LogError(exc);
        //        }

        //        if (socket != null && IsConnected(socket))
        //            return;

        //        var newPeriod = Math.Min(_sendingPeriod.TotalSeconds * 2, _maxSendingPeriod.TotalSeconds);
        //        _sendingPeriod = TimeSpan.FromSeconds(newPeriod);

        //        LogDiagnosticInfo(string.Format("Connection to the server lost. Re-try in {0} seconds.", newPeriod));

        //        Disconnect();
        //    }
        //}

        //public void Flush(double maxTimeSecs = 10)
        //{
        //    LogDiagnosticInfo("RemoteSyslog5424Appender.Flush({0}, {1})", Name, maxTimeSecs);

        //    var thread = new Thread(TrySendMessages);
        //    thread.Start();

        //    if (!thread.Join(TimeSpan.FromSeconds(maxTimeSecs)))
        //    {
        //        thread.Interrupt();
        //        if (!thread.Join(TimeSpan.FromSeconds(0.1)))
        //            thread.Abort();
        //    }
        //}

        //private static readonly SocketError[] IgnoreSocketErrors = {
        //    SocketError.TimedOut, SocketError.ConnectionRefused
        //};

        public void Disconnect()
        {
            lock (_connectionSync)
            {
                if (_writer != null)
                {
                    try
                    {
                        _writer.Dispose();
                    }
                    catch (Exception exc)
                    {
                        LogError(exc);
                    }
                    _writer = null;
                }

                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }

                if (_socket != null)
                {
                    try
                    {
                        if (_socket.Connected)
                            _socket.Disconnect(true);
                    }
                    catch (Exception exc)
                    {
                        LogError(exc);
                    }

                    _socket.Dispose();
                    _socket = null;
                }
            }
        }

        static bool IsConnected(Socket socket)
        {
            return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        }

        public void Dispose()
        {
            //try
            //{
            //    LogDiagnosticInfo("RemoteSyslog5424Appender.Dispose()");

            //    _closing = true;

            //    // give the sender thread some time to flush the messages
            //    _senderThread.Join(TimeSpan.FromSeconds(2));

            //    _senderThread.Interrupt();
            //    _senderThread.Join(TimeSpan.FromSeconds(1));

            //    _senderThread.Abort();

            //    _disposed = true;
            //}
            //catch (ThreadStateException)
            //{
            //}
            //catch (Exception exc)
            //{
            //    LogError(exc);
            //}
            //try
            //{
            //    Disconnect();
            //}
            //catch (Exception exc)
            //{
            //    LogError(exc);
            //}
        }

        //protected override void OnClose()
        //{
        //    // note that total time for all AppDomain.ProcessExit handlers is limited by runtime, 2 seconds by default
        //    // https://msdn.microsoft.com/en-us/library/system.appdomain.processexit(v=vs.110).aspx
        //    LogDiagnosticInfo("RemoteSyslog5424Appender.OnClose()");
        //    Dispose();
        //    base.OnClose();
        //}

        void LogError(string format, params object[] args)
        {
        //    var message = string.Format(format, args);
        //    if (_closing)
        //        Trace.WriteLine(message);
        //    else
        //        _log.Error(message);

        //    RemoteLog(message, Level.Error);
        }

        void LogError(Exception exc)
        {
            LogError(exc.ToString());
        }

        //void LogDiagnosticInfo(string format, params object[] args)
        //{
        //    try
        //    {
        //        var message = string.Format(format, args);
        //        if (_closing)
        //            Trace.WriteLine(message);
        //        else
        //            _log.Info(message);

        //        RemoteLog(message, Level.Info);
        //    }
        //    catch (Exception exc)
        //    {
        //        Trace.WriteLine(exc.ToString());
        //    }
        //}

        //private void RemoteLog(string message, Level level)
        //{
        //    if (EnableRemoteDiagnosticInfo)
        //    {
        //        lock (_messageQueue)
        //        {
        //            var loggingEvent = CreateLoggingEvent(message, level);
        //            var renderedMessage = FormatMessage(loggingEvent, _diagFields);
        //            _messageQueue.Enqueue(renderedMessage);
        //        }
        //    }
        //}

        //private readonly Dictionary<string, string> _diagFields = new Dictionary<string, string> { { "category", "diagnostic" } };

        //public static void Flush(string appenderName, double maxTimeSecs = 10)
        //{
        //    var hierarchy = (Hierarchy)LogManager.GetRepository();
        //    var temp = hierarchy.GetAppenders().First(cur => cur.Name == appenderName);
        //    var appender = (RemoteSyslog5424Appender)temp;
        //    appender.Flush(maxTimeSecs);
        //}

        //private void LogStartupInfo()
        //{
        //    var entryAssembly = Assembly.GetEntryAssembly();
        //    var message = string.Format("Starting '{0}' '{1}",
        //        (entryAssembly != null) ? Assembly.GetEntryAssembly().FullName : Process.GetCurrentProcess().MainModule.FileName,
        //        Assembly.GetExecutingAssembly().FullName);
        //    LogDiagnosticInfo(message);
        //}

        //private string Escape(string val)
        //{
        //    if (TrailerChar == null)
        //        return val;

        //    var ch = TrailerChar.Value;
        //    if (ch == '\r' || ch == '\n')
        //        return val.Replace("\r", "\\r").Replace("\n", "\\n");
        //    return val.Replace(new string(ch, 1), string.Format("\\u{0}", ((int)ch).ToString("X4")));
        //}

        private Socket _socket;
        private EndPoint _remoteEndpoint;
        private Stream _stream;
        private TextWriter _writer;

        private volatile bool _disposed;
        private volatile bool _closing;
        
        private readonly Queue<string> _messageQueue = new Queue<string>();

        private readonly object _connectionSync = new object();
        private readonly object _sendingSync = new object();

        private readonly Thread _senderThread;

        private TimeSpan _sendingPeriod;
        private readonly TimeSpan _defaultSendingPeriod = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _maxSendingPeriod = TimeSpan.FromMinutes(10);

    }
}