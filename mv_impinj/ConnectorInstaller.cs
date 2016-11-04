using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Xml;

namespace mv_impinj
{
    [RunInstaller(true)]
    public sealed partial class ConnectorInstaller : System.ServiceProcess.ServiceInstaller
    {
       private readonly EventLog _logger = new EventLog("Application", ".", "mv_impinj_installer");


        public ConnectorInstaller()
        {
            this.Description =
                "Connector Service to send RAIN RFID data from Impinj platform to MobileView Generic Gateway";
            this.DisplayName = "Impinj Connector for MobileView";
            this.ServiceName = "mv_impinj_connector";
            this.StartType = System.ServiceProcess.ServiceStartMode.Manual;
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);
            _logger.WriteEntry("Installing Connector Services",EventLogEntryType.Information,10,2);
            var propertyKeys = getPropertyKeys();
            var doc = new XmlDocument();
            var appConfigPath = LoadConfigurattion(doc);
            var appSettingsNode = FindAppSettingsNode(doc);
            if (appSettingsNode != null)
                propertyKeys.ForEach(key => SetPropertyValue(appSettingsNode, key, Context.Parameters[key]));
            var timer = 0;
            if(int.TryParse(Context.Parameters["AmqpNoiseTimer"], out timer))
                SetPropertyValue(appSettingsNode,"AmqpNoiseTimer",(timer*1000).ToString());
            else
                _logger.WriteEntry($"Supplied AMQP Noise Timer value ({Context.Parameters["AmqpNoiseTimer"]}) is not parsable as an integer. falling back on the default of 2 seconds",EventLogEntryType.Warning,404,101);
            doc.Save(appConfigPath);

        }
        private List<string> getPropertyKeys()
        {
            return new List<string> { "ItemSenseUrl", "ItemSenseUser", "ItemSensePassword","MobileViewZoneMap", "MobileViewBase", "HttpsCertificates", "MobileViewPrefix" };
        }

        /// <summary>
        /// obi
        /// </summary>
        /// <param name="appSettingsNode"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void SetPropertyValue(XmlNode appSettingsNode, string key, string value)
        {
            var node =
                appSettingsNode.ChildNodes.Cast<XmlNode>()
                    .FirstOrDefault(n => n?.Attributes?["key"] != null && n.Attributes["key"].Value == key);
            if (node?.Attributes?["value"] != null) node.Attributes["value"].Value = value;
        }

        private XmlNode FindChild(XmlNode n, string tagName)
        {
            return n.ChildNodes.Cast<XmlNode>().FirstOrDefault(nChildNode => nChildNode.Name.Equals(tagName));
        }

        private XmlNode FindAppSettingsNode(XmlDocument doc)
        {
            return FindChild(doc.DocumentElement, "appSettings");
        }

        private string LoadConfigurattion(XmlDocument doc)
        {
            string assemblypath = Context.Parameters["assemblypath"];
            string appConfigPath = assemblypath + ".config";
            doc.Load(appConfigPath);
            return appConfigPath;
        }

    }

    [RunInstaller(true)]
    public sealed class ConnectorProccessInstaller : ServiceProcessInstaller
    {
        public ConnectorProccessInstaller()
        {
            this.Account = ServiceAccount.NetworkService;           
        }

    }
}