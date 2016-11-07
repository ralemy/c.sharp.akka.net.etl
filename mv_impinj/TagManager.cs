using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Akka.Actor;

namespace mv_impinj
{
    internal class TagManager : ReceiveActor
    {
        readonly Dictionary<string, IActorRef> _tagProcessors = new Dictionary<string, IActorRef>();
        private readonly int _noiseTimer;




        public TagManager(int amqpNoiseTimer)
        {
            _noiseTimer = amqpNoiseTimer;
            var processor = new Action<IMobileViewReportable>(m => GetEpcProcessor(m.Epc).Tell(m, ActorRefs.NoSender));

            Receive<AmqpMessage>(processor);
            Receive<List<ImpinjItem>>(message => message.ForEach(m => processor(m)));
        }





        private IActorRef GetEpcProcessor(string epc)
        {
            if (!_tagProcessors.ContainsKey(epc))
                _tagProcessors.Add(epc, Context.ActorOf(TagProcessor.Props(epc, _noiseTimer), epc));
            return _tagProcessors[epc];
        }





        public static Props Props(NameValueCollection appSettings)
        {
            return Akka.Actor.Props.Create<TagManager>(int.Parse(appSettings["AmqpNoiseTimer"]));
        }
    }
}