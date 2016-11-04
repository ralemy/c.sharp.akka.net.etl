namespace mv_impinj
{
    internal interface IAmqpMsgProcessor
    {
        void OnMessage(string amqpMsg);
    }
}