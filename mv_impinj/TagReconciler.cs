using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mv_impinj
{
    internal class TagReconciler : ReceiveActor
    {

        public TagReconciler(int heartbeat, Func<string,List<ImpinjItem>> getItmesFunc)
        {
            var minuteTicks = TimeSpan.FromSeconds(heartbeat).Ticks;
            var history = 15 + (2*heartbeat);
            var tagManager = Context.ActorSelection("/user/TagManager");
            Receive<string>(
                message =>
                {
                    var now = DateTime.Now.ToUniversalTime().Ticks;
                    var refTime = DateTime.Now.AddSeconds(-history).ToUniversalTime();
                    var tags = getItmesFunc(refTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    foreach (var tag in tags
                        .Where(t => now - t.Time.Ticks > minuteTicks))
                        tag.Zone = "ABSENT";
                    tagManager.Tell(tags, ActorRefs.NoSender);
                });
        }

        public static Props Props(int heartbeat, Func<string, List<ImpinjItem>> getItmesFunc) => Akka.Actor.Props.Create<TagReconciler>(heartbeat,getItmesFunc);
    }
}