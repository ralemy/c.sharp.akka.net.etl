using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
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
            this.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            _logger.WriteEntry("Installing Connector Services",EventLogEntryType.Information,10,2);

            var propertyDictionary = CreatePropertyDictionary(getPropertyKeys(), Context.Parameters);

            UpdateProperties(propertyDictionary, Context.Parameters["assemblypath"]);
        }

        private static void UpdateProperties(StringDictionary propertyDictionary, string assemblyPath)
        {
            var doc = new XmlDocument();
            var appConfigPath = LoadConfigurattion(doc,assemblyPath);
            var appSettingsNode = FindAppSettingsNode(doc);
            if (appSettingsNode != null)
                foreach (var key in propertyDictionary.Keys)
                {
                    SetPropertyValue(appSettingsNode, key.ToString(), propertyDictionary[key.ToString()]);
                }
            doc.Save(appConfigPath);
        }

        private StringDictionary CreatePropertyDictionary(List<string> keys, StringDictionary dict)
        {
            var result = new StringDictionary();
            int port;
            keys.ForEach(key =>
            {
                switch (key)
                {
                    case "ConfigurationPort":
                        if(dict.ContainsKey(key))
                            if(int.TryParse(dict[key],out port))
                                if(port>0 && port<65535)
                                   result.Add(key,dict[key]);
                        break;
                    default:
                        result.Add(key, dict.ContainsKey(key) ? dict[key] : "");
                        break;
                }
            });
            return result;
        }

        private List<string> getPropertyKeys()
        {
            return new List<string> { "ConfigurationPort" };
        }

        private static void SetPropertyValue(XmlNode appSettingsNode, string key, string value)
        {
            var node =
                appSettingsNode.ChildNodes.Cast<XmlNode>()
                    .FirstOrDefault(n => n?.Attributes?["key"] != null && n.Attributes["key"].Value.ToLower() == key);
            if (node?.Attributes?["value"] != null) node.Attributes["value"].Value = value;
        }

        private static XmlNode FindChild(XmlNode n, string tagName)
        {
            return n.ChildNodes.Cast<XmlNode>().FirstOrDefault(nChildNode => nChildNode.Name.Equals(tagName));
        }

        private static XmlNode FindAppSettingsNode(XmlDocument doc)
        {
            return FindChild(doc.DocumentElement, "appSettings");
        }

        private static string LoadConfigurattion(XmlDocument doc, string assemblyPath)
        {
            var appConfigPath = assemblyPath + ".config";
            doc.Load(appConfigPath);
            return appConfigPath;
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            using (var sc = new ServiceController(this.ServiceName))
            {
                sc.Start();
            }
        }
    }

    [RunInstaller(true)]
    public sealed class ConnectorProccessInstaller : ServiceProcessInstaller
    {
        public ConnectorProccessInstaller()
        {
            this.Account = ServiceAccount.LocalSystem;           
        }

    }
}