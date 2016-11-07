using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Configuration;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using Akka.Actor;
using Akka;

namespace mv_impinj
{
    partial class ConnectorService : ServiceBase
    {
        private ItemSenseProxy _itemSense;
        private HttpServer _server;
        private ConnectorActors _actorSystem;
        private EventLog _eventLog;
        public bool IsRunning = false;
        private Inbox _inbox;

        public void Startup()
        {
            var appSettings = ConfigurationManager.AppSettings;
            IsRunning = false;
            _eventLog = new EventLog("Application", ".", "mv_impinj_connector");
            _server = new HttpServer(appSettings, this, _eventLog);
            _server.Start();
        }


        public void Run(NameValueCollection appSettings)
        {
            if (IsRunning) return;
            IsRunning = true;
            Report.Prefix = appSettings["MobileViewPrefix"];
            _actorSystem = new ConnectorActors("PI2MV", appSettings, _eventLog);
            _itemSense = new ItemSenseProxy(appSettings);
            _inbox = Inbox.Create(_actorSystem.System);
            _actorSystem.ReportZoneMap(_itemSense.GetZoneMap(appSettings["MobileViewZoneMap"]));
            _itemSense.ConsumeQueue(new AmqpRegistrationParams(),_actorSystem.ProcessAmqp());
            _actorSystem.StartReconciliation(fromTime => _itemSense.GetRecentItems(fromTime));
        }

        public string GetReport(string key)
        {
            try
            {
                switch (key)
                {
                    case "ItemSenseReceived":
                        return _itemSense.ReceivedMessages.ToString();
                    case "ItemSenseReconRun":
                        return _itemSense.ReconcileRuns.ToString();
                    case "MobileViewReported":
                        _inbox.Send(_actorSystem.Reporter, "status");
                        return (string) _inbox.Receive(TimeSpan.FromSeconds(5));
                    default:
                        return $"No report available for {key}";
                }
            }
            catch (System.TimeoutException e)
            {
                return "Reporter is not responding. check connection to Mobile View";
            }
            catch (Exception e)
            {
                return e.GetType().AssemblyQualifiedName;
            }
        }

        public void Shutdown()
        {
            _itemSense?.ReleaseQueue();
            _actorSystem?.Terminate();
            _server?.Stop();
        }

        public ConnectorService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.Startup();
        }

        protected override void OnStop()
        {
            this.Shutdown();
        }
    }
}