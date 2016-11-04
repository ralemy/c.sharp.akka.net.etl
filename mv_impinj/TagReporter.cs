using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using Akka.Actor;

namespace mv_impinj
{
    internal class TagReporter : ReceiveActor
    {
        private readonly NameValueCollection _config;
        private readonly WebClient _client;
        private readonly UriBuilder _reportEndpoint;
        private readonly ActorSelection _logger;
        private readonly UriBuilder _zoneEndpoint;
        private readonly XmlMarshaller _marshaller;

        public TagReporter(NameValueCollection appSettings)
        {
            var nl = Environment.NewLine;
            _config = appSettings;
            _client = new WebClient();
            _reportEndpoint = new UriBuilder(appSettings["MobileViewBase"] + appSettings["MobileViewReports"]);
            _zoneEndpoint = new UriBuilder(appSettings["MobileViewBase"] + appSettings["MobileViewLocations"]);
            _logger = Context.ActorSelection("/user/Logger");
            _marshaller = new XmlMarshaller();
            if (appSettings["HttpsCertificates"].Equals("ignore"))
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Receive<AmqpMessageDetails>(
                message =>
                {
                    var postData = _marshaller.MarshallToReport(message.Zone,message.Epc);
                    ForwardToMobileView(postData, _reportEndpoint);
                });
            Receive<ZoneMap>(
                zoneMap =>
                {
                    var payload = _marshaller.MarshallToLocations(zoneMap.Zones.Select(z => z.Name).ToList());
                    ForwardToMobileView(payload, _zoneEndpoint);
                });
        }

        protected override void PostStop()
        {
            lock (_client)
            {
                _client?.Dispose();
            }
        }

        private void ForwardToMobileView(string postData, UriBuilder endpoint)
        {
            try
            {
                lock (_client)
                {
                    _client.UploadString(endpoint.Uri, postData);
                }
            }
            catch (WebException e)
            {
                _logger.Tell(e);
            }
        }

        public static Props props(NameValueCollection appSettings)
        {
            return Props.Create<TagReporter>(appSettings);
        }
    }
}