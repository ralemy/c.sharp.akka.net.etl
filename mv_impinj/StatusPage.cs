using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace mv_impinj
{
    class StatusPage
    {
        private readonly HttpServer _server;

        public StatusPage(HttpServer server)
        {
            _server = server;
        }

        public void ServeStatus(HttpListenerContext c)
        {
            try
            {

                var doc = new HtmlDocument();
                doc.LoadHtml(Properties.Resources.status);
                foreach (var htmlNode in doc.DocumentNode.Descendants()
                        .Where(n => n.Name.Equals("td"))
                        .Where(n => n.GetAttributeValue("id", "none") != "none")
                )
                {
                    var key = htmlNode.GetAttributeValue("id", "none");
                    switch (key)
                    {
                        case "none":
                        case "ItemSenseReceived":
                        case "ItemSenseReconRun":
                            break;
                        default:
                            htmlNode.InnerHtml = _server.AppSettings[key] == null ? "" : _server.AppSettings[key];
                            break;
                    }
                }
                _server.SendResponse(c.Response, doc.DocumentNode.OuterHtml);
            }
            catch (Exception e)
            {
                _server.SendResponse(c.Response, e.Message);
            }
        }

        public void ServeStats(HttpListenerContext c)
        {
            if (!_server.IsConnectorRunning())
            {
                _server.SendResponse(c.Response, "{}", "application/json");
                return;
            }
            var keys = new List<string>() { "ItemSenseReceived", "ItemSenseReconRun", "MobileViewReported" };
            var response = keys.Aggregate("", (acc, k) => acc + $@", ""{k}"" : ""{_server.GetReport(k)}"" ");
            _server.SendResponse(c.Response, "{" + response.Substring(1) + "}", "application/json");
        }


    }
}
