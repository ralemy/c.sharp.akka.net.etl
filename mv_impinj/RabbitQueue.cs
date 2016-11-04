using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace mv_impinj
{
    internal class RabbitQueue
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly EventingBasicConsumer _consumer;

        public RabbitQueue(ConnectionFactory factory)
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _consumer = new EventingBasicConsumer(_channel);
        }


        public void AddReceiver(EventHandler<BasicDeliverEventArgs> action)
        {           
            _consumer.Received += action;
        }

        public void Consume(string queue)
        {
            _channel.BasicConsume(queue: queue,
                noAck: true,
                consumer: _consumer);
        }

        public void ReleaseQueue()
        {
            if(_channel != null && _channel.IsOpen)
            _channel.Close();
            if(_connection != null && _connection.IsOpen)
            _connection.Close();
        }
    }
}