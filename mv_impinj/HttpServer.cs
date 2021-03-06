﻿using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace mv_impinj
{
    internal class DisposableImpl : IDisposable
    {
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool v)
        {
            if (_disposed) return;
            if (v) FreeManagedResources();
            _disposed = true;
        }

        protected virtual void FreeManagedResources()
        {
        }

        ~DisposableImpl()
        {
            Dispose(false);
        }
    }

    class HttpServer : DisposableImpl
    {
        private readonly int _port;
        private readonly ConfigPage _configPage;
        private readonly ConnectorService _connectorService;


        private HttpListener _listener;
        private Thread _server;
        public NameValueCollection AppSettings;
        public string FlashMessage;
        public bool ConfigSaved;
        private readonly StatusPage _statusPage;

        public EventLog Logger { get; private set; }

        public HttpServer(NameValueCollection appSettings, ConnectorService connectorService, EventLog eventLog)
        {
            ConfigSaved = false;
            FlashMessage = "";
            AppSettings = appSettings;
            _port = int.Parse(appSettings["ConfigurationPort"]);
            _connectorService = connectorService;
            _configPage = new ConfigPage(this);
            _statusPage = new StatusPage(this);
            Logger = eventLog;
        }


        public void Start()
        {
            if (_server != null) throw new Exception("Already Started");
            _server = new Thread(Listen);
            _server.Start();
        }


        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            try
            {
                while (true)
                    Process(_listener.GetContext());
            }
            catch
            {
                // ignored
            }
        }

        protected virtual void Process(HttpListenerContext c)
        {
            var request = c.Request;
            if (request.HttpMethod.Equals(HttpMethod.Get.Method))
                ServeFiles(c);
            else if (request.Url.AbsolutePath.EndsWith("/service/run"))
            {
                try
                {
                    if (!_connectorService.IsRunning)
                        _connectorService.Run(AppSettings);
                }
                catch (Exception e)
                {
                    _connectorService.IsRunning = false;
                    _configPage.SetConfigResult(e.Message + " Check Itemsense options.",false);
                }
                RedirectToIndex(c);
            }
            else
                _configPage.SetConfigs(c);
        }

        private void ServeFiles(HttpListenerContext c)
        {
            var path = c.Request.Url.AbsolutePath;
            if (path.EndsWith("/bootstrap.min.css"))
                SendResponse(c.Response, Properties.Resources.bootstrap_min, "text/css");
            else if (path.EndsWith("/logo.png"))
                SendResponse(c.Response, Properties.Resources.logo, ImageFormat.Png);
            else if (path.EndsWith("/report"))
                _statusPage.ServeStats(c);
            else
                ServeIndex(c);
        }


        public void RedirectToIndex(HttpListenerContext c)
        {
            c.Response.Redirect("/");
            c.Response.Close();
        }


        private void ServeIndex(HttpListenerContext c)
        {
            if (_connectorService.IsRunning)
                _statusPage.ServeStatus(c);
            else
                _configPage.ServeConfig(c);
        }

        public void SendResponse(HttpListenerResponse cResponse, string file, string mimeType)
        {
            cResponse.ContentType = mimeType;
            SendResponse(cResponse, file);
        }

        public void SendResponse(HttpListenerResponse cResponse, Bitmap buffer, ImageFormat mime)
        {
            //            cResponse.Headers[HttpRequestHeader.CacheControl] = "no-cache";
            //            cResponse.Headers[HttpRequestHeader.Pragma] = "no-cache";
            cResponse.AddHeader("Cache-Control", "no-cache");
            cResponse.AddHeader("Pragma", "no-cache");
            buffer.Save(cResponse.OutputStream, mime);
            cResponse.Close();
        }

        public void SendResponse(HttpListenerResponse r, string s)
        {
            SendResponse(r, Encoding.UTF8.GetBytes(s));
        }

        public void SendResponse(HttpListenerResponse response, byte[] buffer)
        {
            //            response.Headers[HttpRequestHeader.CacheControl] = "no-cache";
            //            response.Headers[HttpRequestHeader.Pragma] = "no-cache";
            response.AddHeader("Cache-Control", "no-cache");
            response.AddHeader("Pragma", "no-cache");
            var output = response.OutputStream;
            response.ContentLength64 = buffer.Length;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        public void Stop()
        {
            _listener?.Stop();
            _server?.Abort();
            _listener = null;
            _server = null;
        }

        protected override void Dispose(bool v)
        {
            Stop();
            base.Dispose(v);
        }

        public bool IsConnectorRunning()
        {
            return _connectorService.IsRunning;
        }

        public string GetReport(string key)
        {
            return _connectorService.GetReport(key);
        }
    }
}