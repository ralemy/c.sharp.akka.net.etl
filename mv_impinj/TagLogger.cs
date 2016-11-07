using System.Diagnostics;
using System.Net;
using Akka.Actor;

namespace mv_impinj
{
    internal class TagLogger:ReceiveActor
    {
        public TagLogger(EventLog logger)
        {
            Receive<WebException>(message =>
            {
                logger.WriteEntry(message.Message, EventLogEntryType.Information, 2, 2);

            });
            ReceiveAny(message =>
            {
                logger.WriteEntry(message.ToString(),EventLogEntryType.Information,2,2);
            });
        }
        public static Props Props(EventLog eventLog)
        {
            return Akka.Actor.Props.Create<TagLogger>(eventLog);
        }
    }
}