using Akka.Actor;
using Newtonsoft.Json;

namespace mv_impinj
{
    internal class MessageProcessor : IAmqpMsgProcessor
    {
        private readonly ActorSelection _tagManager;


        public MessageProcessor(ActorSystem actorSystem)
        {
            this._tagManager = actorSystem.ActorSelection("/user/TagManager");
        }

        public void OnMessage(string amqpMsg)
        {
            _tagManager.Tell(JsonConvert.DeserializeObject<AmqpMessageDetails>(amqpMsg),ActorRefs.NoSender);
        }
    }
}