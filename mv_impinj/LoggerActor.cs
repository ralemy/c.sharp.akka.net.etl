using System.Diagnostics;
using System.Net;
using Akka.Actor;

namespace mv_impinj
{
    internal class LoggerActor:ReceiveActor
    {
        public LoggerActor(EventLog logger)
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
        public static Props props(EventLog eventLog)
        {
            return Props.Create<LoggerActor>(eventLog);
        }
    }
}