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
            Receive<AmqpMessageDetails>(message =>
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
                    Console.WriteLine($"{item.Epc} forced to {item.Zone} from {_location}, {_candidate}, {_state}");
                    if (!_state.Equals("Waiting"))
                        Cache(item);
                    else if (!item.Zone.Equals(_candidate))
                        Cache(item);
                    else
                    {
                        Console.WriteLine($"{item.Epc} already waiting for {item.Zone}");
                    }
                }
                else
                {
                    Console.WriteLine($"{item.Epc} skipped forcing from {_location}");
                }
            });
        }

        private void HandleWaiting(AmqpMessageDetails message)
        {
            if (message.Zone.Equals(_candidate))
                Report(message);
            else
                Cache(message);
        }

        private void HandlePresent(AmqpMessageDetails message)
        {
            if (!message.Zone.Equals(_location))
                Cache(message);
        }

        private void HandleAbsent(AmqpMessageDetails message)
        {
            if (!message.Zone.Equals("ABSENT"))
                Report(message);
        }

        private void HandleInitial(AmqpMessageDetails message)
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
            _t.Change(this._amqpNoiseTimer, Timeout.Infinite);
        }

        private void ReportDelay(object state)
        {
            if (_candidate != null)
                if (!_candidate.Equals(_location))
                    Report(MakeMessage(_candidate));
            StopTimer();
        }

        private AmqpMessageDetails MakeMessage(string candidate)
        {
            return new AmqpMessageDetails
            {
                Epc = this._epc,
                Zone = candidate
            };
        }

        private void StopTimer()
        {
            _candidate = null;
            _t.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public static Props props(string epc, int amqpNoiseTimer)
        {
            return Props.Create<TagProcessor>(epc, amqpNoiseTimer);
        }
    }
}