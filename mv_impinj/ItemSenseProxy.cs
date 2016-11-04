using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Akka.Actor;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace mv_impinj
{
    public class ZonePoint
    {
        [JsonProperty("x")]
        public float X;
        [JsonProperty("y")]
        public float Y;
        [JsonProperty("z")]
        public float Z;

    }

    public class ZoneObject
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("floor")]
        public string Floor;
        [JsonProperty("points")]
        public List<ZonePoint> Points;
    }

    public class ZoneMap
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("facility")]
        public string Facility;
        [JsonProperty("zones")]
        public List<ZoneObject> Zones;
    }

    public class AmqpMessageQueueDetails
    {
        [JsonProperty("serverUrl")]
        public string ServerUrl { get; set; }

        [JsonProperty("queue")]
        public string Queue { get; set; }
    }

    public interface IMobileViewReportable
    {
        string Epc { get; set; }
        string Zone { get; set; }
    }
    public class AmqpMessageDetails : IMobileViewReportable
    {
        [JsonProperty("epc")]
        public string Epc { get; set; }
        [JsonProperty("toZone")]
        public string Zone { get; set; }
    }
    public class ImpinjItem : IMobileViewReportable
    {
        [JsonProperty("epc")]
        public string Epc { get; set; }
        [JsonProperty("zone")]
        public string Zone { get; set; }
        [JsonProperty("lastModifiedTime")]
        public DateTime Time;
    }
    internal static class ItemSenseEndpoints
    {
       public static readonly String MessageQueue = "/itemsense/data/v1/messageQueues/zoneTransition/configure";
        public static readonly String ZoneMap = "/itemsense/configuration/zoneMaps/show";
        public static readonly String GetItems = "/itemsense/data/v1/items/show";
    }
    internal static class WebClientExtension
    {
        public static WebClient SetHeaders(this WebClient wb, string creds, string url)
        {
            wb.Headers[HttpRequestHeader.Authorization] = $"Basic {creds}";
            wb.Headers[HttpRequestHeader.ContentType] = "application/json";
            wb.BaseAddress = url;
            return wb;
        }

        public static T Post<T>(this WebClient wb, string endpoint, dynamic payload)
        {
            var result = wb.UploadString(endpoint,"POST", JsonConvert.SerializeObject(payload));
            return JsonConvert.DeserializeObject<T>(result);
        }

        public static T Get<T>(this WebClient wb, string endpoint)
        {
            var result = wb.DownloadString(endpoint);
            return JsonConvert.DeserializeObject<T>(result);
        }
    }

    class ItemSenseProxy
    {
        private readonly string _password;
        private readonly string _url;
        private readonly string _user;
        private readonly string _credentials;
        private RabbitQueue _rabbitQueue;


        public ItemSenseProxy(String url, String user, String password)
        {
            this._url = ensureCorrectFormat(url);
            this._password = password;
            this._user = user;
            this._credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password));

        }

        private string ensureCorrectFormat(string url)
        {
            //ToDo: this needs to make sure that protocol is http:// and there is no trailing slashes.
            return url;
        }

        private WebClient InitClient()
        {
            return new WebClient()
            {
                Headers =
                {
                    [HttpRequestHeader.Authorization] = $"Basic {_credentials}",
                    [HttpRequestHeader.ContentType] = "application/json"
                }
            };
        }


        public AmqpMessageQueueDetails RegisterQueue(Object parameters)
        {
            if (parameters == null)
                parameters = new { };

            using (WebClient webClient = new WebClient().SetHeaders(_credentials,_url))
            {
                return webClient.Post<AmqpMessageQueueDetails>(ItemSenseEndpoints.MessageQueue, parameters);
            };
        }

        internal void ListenToQueue(AmqpMessageQueueDetails queueParams, ConnectorService.MsgListener msgListener)
        {
           
            var factory = new ConnectionFactory()
            {
               HostName = _url.Replace("http://",string.Empty), Port = 5672,
                AutomaticRecoveryEnabled = true,
                VirtualHost = "/",
                UserName = _user,
                Password = _password
            };
            _rabbitQueue = new RabbitQueue(factory);
            _rabbitQueue.AddReceiver((model, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body);
                msgListener(message);
            });
            _rabbitQueue.Consume(queueParams.Queue);
        }

        public void ReleaseQueue()
        {
            _rabbitQueue.ReleaseQueue();
        }

        public void SetLocations(IActorRef reporter, string zoneMap)
        {
            using (WebClient webClient = new WebClient().SetHeaders(_credentials, _url))
            {
                var zones = webClient.Get<ZoneMap>(ItemSenseEndpoints.ZoneMap+"/"+zoneMap);
                reporter.Tell(zones,ActorRefs.NoSender);
            }
        }

        public List<ImpinjItem> GetRecentItems(string fromTime)
        {
            using (WebClient webClient = new WebClient().SetHeaders(_credentials, _url))
            {
                return webClient.Get<ItemsObject>(ItemSenseEndpoints.GetItems + "?pageSize=1000&fromTime="+fromTime).Items;
            }
        }
    }

    public class ItemsObject
    {
        [JsonProperty("items")] public List<ImpinjItem> Items;
        [JsonProperty("nextPageMarker")] public string NextPageMarker;
    }

   
}

