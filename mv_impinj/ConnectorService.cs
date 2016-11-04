using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Akka.Actor;
using Akka;

namespace mv_impinj
{
   

    partial class ConnectorService : ServiceBase
    {
        public delegate void MsgListener(string amqpMsg);

        private ItemSenseProxy _itemSense;

        public void Startup()
        {
            var appSettings = ConfigurationManager.AppSettings;
            _itemSense = new ItemSenseProxy(appSettings["ItemSenseUrl"], appSettings["ItemSenseUser"],
                appSettings["ItemSensePassword"]);
            Report.Prefix = appSettings["MobileViewPrefix"];
            var actorSystem = ActorSystem.Create("PI2MV");
            var reporter = actorSystem.ActorOf(TagReporter.props(appSettings), "TagReporter");
            _itemSense.SetLocations(reporter, appSettings["MobileViewZoneMap"]);
            actorSystem.ActorOf(TagManager.props(appSettings), "TagManager");
            actorSystem.ActorOf(LoggerActor.props(new EventLog("Application", ".", "mv_impinj_connector")), "Logger");
            var reconciler = actorSystem.ActorOf(TagReconciler.props(_itemSense,2.5f), "TagReconciler");
            actorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(1), reconciler, "Reconcile",ActorRefs.NoSender);
            var msgProcessor = new MessageProcessor(actorSystem);
            var msgListener = new MsgListener(msgProcessor.OnMessage);
            var queueParams = _itemSense.RegisterQueue(null);
            _itemSense.ListenToQueue(queueParams, msgListener);
        }

        public void Shutdown()
        {
            _itemSense.ReleaseQueue();
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