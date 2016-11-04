using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Akka.Actor;

namespace mv_impinj
{
    internal class TagManager:ReceiveActor
    {
        Dictionary<string, IActorRef> tagProcessors = new Dictionary<string, IActorRef>();
        private readonly int _noiseTimer;

        public TagManager(int amqpNoiseTimer)
        {
            this._noiseTimer = amqpNoiseTimer;           
            Receive<AmqpMessageDetails>(message =>
            {
                getEpcProcessor(message.Epc).Tell(message,ActorRefs.NoSender);
            });
            Receive<List<ImpinjItem>>(message =>
            {
                message.ForEach(m =>
                {
                    getEpcProcessor(m.Epc).Tell(m, ActorRefs.NoSender);
                });
            });
        }
        private IActorRef getEpcProcessor(string epc)
        {
            if (!tagProcessors.ContainsKey(epc))
                tagProcessors.Add(epc, Context.ActorOf(TagProcessor.props(epc,_noiseTimer), epc));
            return tagProcessors[epc];
        }
        public static Props props(NameValueCollection appSettings)
        {
           return Props.Create<TagManager>(int.Parse(appSettings["AmqpNoiseTimer"]));
        }
    }
}