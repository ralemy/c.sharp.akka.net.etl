using Akka.Actor;
using System;

namespace mv_impinj
{
    internal class TagReconciler : ReceiveActor
    {
        public static Props props(ItemSenseProxy itemSense, float heartbeat)
        {
            return Props.Create<TagReconciler>(itemSense, heartbeat);
        }

        public TagReconciler(ItemSenseProxy itemSense, float heartbeat)
        {
            var minuteTicks = TimeSpan.FromSeconds(75).Ticks;
            var tagManager = Context.ActorSelection("/user/TagManager");
            Receive<string>(
                message =>
                {
                    var now = DateTime.Now.ToUniversalTime().Ticks;
                    var refTime = DateTime.Now.AddMinutes(-heartbeat).ToUniversalTime();
                    var tags = itemSense.GetRecentItems(refTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    Console.WriteLine($"tags: {tags.Count} at {refTime.ToString("yyyy-MM-ddTHH:mm:ssZ")}");
                    tags.ForEach(t =>
                    {
                        Console.WriteLine($"{t.Epc} in {t.Zone} at diff:{now - t.Time.Ticks > minuteTicks}");
                        if (now - t.Time.Ticks > minuteTicks)
                            t.Zone = "ABSENT";
                    });
                    tagManager.Tell(tags, ActorRefs.NoSender);
                });
        }
    }
}