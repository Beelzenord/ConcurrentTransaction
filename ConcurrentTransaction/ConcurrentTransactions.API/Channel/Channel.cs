namespace ConcurrentTransactions.API.Channel;

public class Channel
{
    private readonly ProducerBuffer _producerBuffer;
    private readonly ConsumerBuffer _consumerBuffer;

    public Channel(ProducerBuffer producer, ConsumerBuffer consumer)
    {
        _producerBuffer = producer;
        _consumerBuffer = consumer;
    }
}
