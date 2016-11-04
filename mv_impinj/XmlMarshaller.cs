using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace mv_impinj
{
    [XmlRoot("locations")]
    public class Locations
    {
        [XmlElement("passiveLocation")] public string[] Location;
    }


    [XmlRoot("reports")]
    public class Reports
    {
        [XmlElement("report")] public Report[] Report;
    }
    public class Report
    {
        private string _tagId;
        [XmlElement("tagType")]
        public string TagType = "PASSIVE";

        [XmlElement("tagId")]
        public string TagId
        {
            get { return "epc:" + _tagId; }
            set { _tagId = value;  }
        }

        [XmlElement("passiveLocation")]
        public string PassiveLocation;
    }
    

    class XmlMarshaller
    {
        private readonly XmlSerializer _reportSerializer;
        private readonly XmlSerializer _locationSerializer;
        private readonly XmlSerializerNamespaces _nameSpace;
        private readonly Reports _reps;

        public string MarshallToReport(string location, string tagId)
        {
            var writer = new StringWriter();
            _reps.Report[0].TagId = tagId;
            _reps.Report[0].PassiveLocation = location;
            _reportSerializer.Serialize(writer, _reps, _nameSpace);
            return writer.GetStringBuilder().ToString();
        }

        public string MarshallToLocations(List<string> names)
        {
            var writer = new StringWriter();
            if(!names.Contains("ABSENT"))
                names.Add("ABSENT");
            var locations = new Locations()
            {
                Location = names.ToArray()
            };
            _locationSerializer.Serialize(writer, locations,_nameSpace);
            return writer.GetStringBuilder().ToString();
        }

        public XmlMarshaller()
        {
            _reportSerializer = new XmlSerializer(typeof(Reports));
            _locationSerializer = new XmlSerializer(typeof(Locations));
            _nameSpace = new XmlSerializerNamespaces();
            _reps = new Reports {Report = new Report[1]};
            _reps.Report[0] = new Report();
            _nameSpace.Add("","");
        }
    }
}