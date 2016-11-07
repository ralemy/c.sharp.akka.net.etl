using System;
using System.Threading;
using Akka.Actor;

namespace mv_impinj
{
    internal class TagProcessor : ReceiveActor
    {
        private string _location;
        private string _candidate;
        private readonly ActorSelection _tagReporter;
        private readonly Timer _t;
        private readonly string _epc;
        private string _state = "initial";
        private readonly int _amqpNoiseTimer;

        public TagProcessor(string epc, int amqpNoiseTimer)
        {
            _epc = epc;
            _amqpNoiseTimer = amqpNoiseTimer;
            _t = new Timer(ReportDelay, null, Timeout.Infinite, Timeout.Infinite);
            _tagReporter = Context.ActorSelection("/user/TagReporter");
            Receive<AmqpMessage>(message =>
            {
                switch (_state)
                {
                    case "initial":
                        HandleInitial(message);
                        break;
                    case "Absent":
                        HandleAbsent(message);
                        break;
                    case "Present":
                        HandlePresent(message);
                        break;
                    case "Waiting":
                        HandleWaiting(message);
                        break;
                }
            });
            Receive<ImpinjItem>(item =>
            {
                if (!item.Zone.Equals(_location))
                {
                    if (!_state.Equals("Waiting"))
                        Cache(item);
                    else if (!item.Zone.Equals(_candidate))
                        Cache(item);
                }
            });
        }

        private void HandleWaiting(AmqpMessage message)
        {
            if (message.Zone.Equals(_candidate))
                Report(message);
            else
                Cache(message);
        }

        private void HandlePresent(AmqpMessage message)
        {
            if (!message.Zone.Equals(_location))
                Cache(message);
        }

        private void HandleAbsent(AmqpMessage message)
        {
            if (!message.Zone.Equals("ABSENT"))
                Report(message);
        }

        private void HandleInitial(AmqpMessage message)
        {
            if (message.Zone.Equals("ABSENT"))
                Report(message);
            else
                Cache(message);
        }

        private void Report(IMobileViewReportable message)
        {
            var newState = message.Zone.Equals("ABSENT") ? "Absent" : "Present";
            _location = message.Zone;
            _state = newState;
            StopTimer();
            _tagReporter.Tell(message, ActorRefs.NoSender);
        }

        private void Cache(IMobileViewReportable message)
        {
            _candidate = message.Zone;
            _state = "Waiting";
            _t.Change(_amqpNoiseTimer, Timeout.Infinite);
        }

        private void ReportDelay(object state)
        {
            if (_candidate != null)
                if (!_candidate.Equals(_location))
                    Report(MakeMessage(_candidate));
            StopTimer();
        }

        private AmqpMessage MakeMessage(string candidate)
        {
            return new AmqpMessage
            {
                Epc = _epc,
                Zone = candidate
            };
        }

        private void StopTimer()
        {
            _candidate = null;
            _t.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public static Props Props(string epc, int amqpNoiseTimer)
        {
            return Akka.Actor.Props.Create<TagProcessor>(epc, amqpNoiseTimer);
        }
    }
}