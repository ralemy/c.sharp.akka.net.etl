using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace mv_impinj
{
    
    //Zone Map
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

    
    //AMQP
    public class AmqpRegistrationParams
    {
        [JsonProperty("epc", NullValueHandling=NullValueHandling.Ignore)]
        public string Epc { get; set; }
        [JsonProperty("fromZone", NullValueHandling = NullValueHandling.Ignore)]
        public string FromZone { get; set; }
        [JsonProperty("toZone", NullValueHandling = NullValueHandling.Ignore)]
        public string ToZone { get; set; }
    }
    public class AmqpServerInfo
    {
        [JsonProperty("serverUrl")]
        public string ServerUrl { get; set; }

        [JsonProperty("queue")]
        public string Queue { get; set; }
    }
    public interface ITargetReportable
    {
        string Epc { get; set; }
        string Zone { get; set; }
    }
    public class AmqpMessage : ITargetReportable
    {
        [JsonProperty("epc")]
        public string Epc { get; set; }
        [JsonProperty("toZone")]
        public string Zone { get; set; }
    }


    //GET Items
    public class ImpinjItem : ITargetReportable
    {
        [JsonProperty("epc")]
        public string Epc { get; set; }
        [JsonProperty("zone")]
        public string Zone { get; set; }
        [JsonProperty("lastModifiedTime")]
        public DateTime Time;
    }
    public class ItemsObject
    {
        [JsonProperty("items")]
        public List<ImpinjItem> Items;
        [JsonProperty("nextPageMarker")]
        public string NextPageMarker;
    }

}
