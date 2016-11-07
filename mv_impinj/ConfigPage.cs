using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using HtmlAgilityPack;

namespace mv_impinj
{
    class ConfigPage
    {
        private readonly HttpServer _server;

        public ConfigPage(HttpServer server)
        {
            _server = server;
        }
        public void ServeConfig(HttpListenerContext c)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(Properties.Resources.config);
            foreach (var htmlNode in doc.DocumentNode.Descendants()
                    .Where(n => n.Name.Equals("input"))
            )
                AssignInitialValue(htmlNode, htmlNode.GetAttributeValue("name", "none"));
            AddFlashMessage(doc);
            _server.SendResponse(c.Response, doc.DocumentNode.OuterHtml);
        }

        private void AddFlashMessage(HtmlDocument doc)
        {
            if (_server.FlashMessage != "")
                doc.DocumentNode
                    .Descendants()
                    .First(n => !n.GetAttributeValue("Flash", "none").Equals("none"))
                    .InnerHtml = String.Format(Properties.Resources.FlashMsgTemplate,
                    _server.FlashMessage,
                    _server.ConfigSaved ? Properties.Resources.RunButton : "" ,
                    _server.ConfigSaved ? "alert-success" : "alert-warning");
            _server.FlashMessage = "";
        }

        private void AssignInitialValue(HtmlNode htmlNode, string name)
        {
            switch (name)
            {
                case "HttpsCertificates":
                    if (_server.AppSettings["HttpsCertificates"].Equals("ignore"))
                        htmlNode.SetAttributeValue("checked", "true");
                    break;
                case "none":
                    break;
                default:
                    htmlNode.SetAttributeValue("value", _server.AppSettings[name]);
                    break;
            }
        }

        public void SetConfigs(HttpListenerContext c)
        {
            try
            {
                var streamReader = new StreamReader(c.Request.InputStream);
                var options = streamReader.ReadToEnd();
                var opts = options.Split('&').Select(kv => kv.Split('='));
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = config.AppSettings.Settings;
                foreach (var kv in opts)
                    if (settings.AllKeys.Contains(kv[0]))
                        settings[kv[0]].Value = HttpUtility.UrlDecode(kv[1]);
                    else
                        settings.Add(kv[0], kv[1]);
                foreach (var key in _server.AppSettings.AllKeys)
                {
                    _server.AppSettings[key] = settings[key].Value;
                }
                config.Save(ConfigurationSaveMode.Full);
                SetConfigResult("Configuration Saved", true);
            }
            catch (Exception e)
            {
                SetConfigResult(e.Message,false);
            }
            _server.RedirectToIndex(c);
        }

        public void SetConfigResult(string message, bool result)
        {
            _server.FlashMessage = message;
            _server.ConfigSaved = result;
        }


    }
}
