using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Akka.Actor;

namespace mv_impinj
{
    public class ConnectorActors
    {
        public readonly ActorSystem System;
        private readonly NameValueCollection _appSettings;


        public IActorRef Reconciler { get; private set; }
        public IActorRef Logger { get; private set; }
        public IActorRef Manager { get; private set; }
        public IActorRef Reporter { get; private set; }

        public ConnectorActors(string name, NameValueCollection appSettings, EventLog eventLog)
        {
            _appSettings = appSettings;
            System = ActorSystem.Create(name);
            Logger = System.ActorOf(LoggerActor.Props(eventLog), "Logger");
            Reporter = System.ActorOf(TagReporter.Props(appSettings), "TagReporter");
            Manager = System.ActorOf(TagManager.Props(appSettings), "TagManager");
        }

        public void ReportZoneMap(ZoneMap zoneMap)
        {
            Reporter.Tell(zoneMap,ActorRefs.NoSender);
        }

        public Action<AmqpMessage> ProcessAmqp()
        {
            return msg => Manager.Tell(msg, ActorRefs.NoSender);
        }

        public void StartReconciliation(Func<string, List<ImpinjItem>> getItmesFunc)
        {
            int window;
            if(!int.TryParse(_appSettings["ReconcilerWindow"], out window)) window = 60;
            Reconciler = System.ActorOf(TagReconciler.Props(window,getItmesFunc), "TagReconciler");
            System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMinutes(0), TimeSpan.FromSeconds(window), Reconciler,
                "Reconcile", ActorRefs.NoSender);
        }

        public ActorSelection ActorSelection(string path)
        {
            return System.ActorSelection(path);
        }

        public void Terminate()
        {
            System.Terminate();
        }
    }

    
}