using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using Akka.Actor;

namespace mv_impinj
{
    internal class TagReporter : ReceiveActor
    {
        private readonly WebClient _client;
        private readonly UriBuilder _reportEndpoint;
        private readonly ActorSelection _logger;
        private readonly UriBuilder _zoneEndpoint;
        private readonly XmlMarshaller _marshaller;
        private int _counter;
        private string _statusMessage;

        public TagReporter(NameValueCollection appSettings)
        {
            _client = new WebClient();
            _reportEndpoint = new UriBuilder(appSettings["MobileViewBase"] + appSettings["MobileViewReports"]);
            _zoneEndpoint = new UriBuilder(appSettings["MobileViewBase"] + appSettings["MobileViewLocations"]);
            _logger = Context.ActorSelection("/user/Logger");
            _marshaller = new XmlMarshaller();
            _counter = 0;
            _statusMessage = "Current Counter is {0}";

            if (appSettings["HttpsCertificates"].Equals("ignore"))
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Receive<AmqpMessage>(
                message =>
                {
                    var postData = _marshaller.MarshallToReport(message.Zone, message.Epc);
                    _counter += 1;
                    ForwardToMobileView(postData, _reportEndpoint);
                });
            Receive<ZoneMap>(
                zoneMap =>
                {
                    var payload = _marshaller.MarshallToLocations(zoneMap.Zones.Select(z => z.Name).ToList());
                    ForwardToMobileView(payload, _zoneEndpoint);
                });
            Receive<string>( message => Sender.Tell(String.Format(_statusMessage,_counter)));
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
                _statusMessage = e.Message;
                _logger.Tell(e);
            }
        }

        public static Props Props(NameValueCollection appSettings)
        {
            return Akka.Actor.Props.Create<TagReporter>(appSettings);
        }
    }
}