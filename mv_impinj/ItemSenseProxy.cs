using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace mv_impinj
{
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
            var result = wb.UploadString(endpoint, "POST", JsonConvert.SerializeObject(payload));
            return JsonConvert.DeserializeObject<T>(result);
        }

        public static T Get<T>(this WebClient wb, string endpoint)
        {
            var result = wb.DownloadString(endpoint);
            return JsonConvert.DeserializeObject<T>(result);
        }
    }

    public class ItemSenseProxy: IItemSense
    {
        private readonly string _password;
        private readonly string _url;
        private readonly string _user;
        private readonly string _credentials;
        private RabbitQueue _rabbitQueue;

        public int ReceivedMessages;
        public int ReconcileRuns;



        public ItemSenseProxy(NameValueCollection appSettings)
        {
            ReceivedMessages = 0;
            ReconcileRuns = 0;
            _url = EnsureCorrectFormat(appSettings["ItemSenseUrl"]);
            _user = appSettings["ItemSenseUser"];
            _password = appSettings["ItemSensePassword"];
            _credentials = GetCredentials(_user,_password);
        }



        public void ConsumeQueue(AmqpRegistrationParams queueParams, Action<AmqpMessage> reporter )
        {
            ListenToQueue(RegisterQueue(queueParams), reporter);
        }

        private static string GetCredentials(string user, string password)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password));
        }

        private static string EnsureCorrectFormat(string url)
        {
            if (!url.StartsWith("http://"))
                url += "http://";
            if (url.EndsWith("/"))
                url = url.Remove(url.Length - 2);
            return url;
        }


        public AmqpServerInfo RegisterQueue(AmqpRegistrationParams parameters)
        {

            using (WebClient webClient = new WebClient().SetHeaders(_credentials, _url))
            {
                return webClient.Post<AmqpServerInfo>(ItemSenseEndpoints.MessageQueue, parameters);
            }
        }

        internal void ListenToQueue(AmqpServerInfo queueParams, Action<AmqpMessage> reporter)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _url.Replace("http://", string.Empty),
                Port = 5672,
                AutomaticRecoveryEnabled = true,
                VirtualHost = "/",
                UserName = _user,
                Password = _password
            };
            _rabbitQueue = new RabbitQueue(factory);
            _rabbitQueue.AddReceiver((model, e) =>
            {
                var message = JsonConvert.DeserializeObject<AmqpMessage>(Encoding.UTF8.GetString(e.Body));
                ReceivedMessages += 1;
                reporter(message);
            });
            _rabbitQueue.Consume(queueParams.Queue);
        }

        public void ReleaseQueue()
        {
            _rabbitQueue?.ReleaseQueue();
        }

        public ZoneMap GetZoneMap(string zoneMap)
        {
            using (WebClient webClient = new WebClient().SetHeaders(_credentials, _url))
            {
                return webClient.Get<ZoneMap>(ItemSenseEndpoints.ZoneMap + "/" + zoneMap);
            }
        }

        public List<ImpinjItem> GetRecentItems(string fromTime)
        {
            ReconcileRuns += 1;
            using (WebClient webClient = new WebClient().SetHeaders(_credentials, _url))
            {
                return webClient.Get<ItemsObject>(ItemSenseEndpoints.GetItems + "?pageSize=1000&fromTime=" + fromTime).Items;
            }
        }
    }


    public interface IItemSense
    {
        ZoneMap GetZoneMap(string name);
        List<ImpinjItem> GetRecentItems(string fromTime);
        void ConsumeQueue(AmqpRegistrationParams queueParams, Action<AmqpMessage> reporter);
    }

    
}

