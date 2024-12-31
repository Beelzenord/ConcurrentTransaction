using ConcurrentTransactions.API.Model;
using System.Collections.Concurrent;
using System.Xml;

namespace ConcurrentTransactions.API.Channel;
public class ProducerBuffer
{
    public ConcurrentDictionary<int, bool> OccupiedClients { get; }
    public ConcurrentQueue<TransactionRequest> Queue { get; }
    private readonly SemaphoreSlim _signal;

    public ProducerBuffer()
    {
        Queue = new ConcurrentQueue<TransactionRequest>();
        OccupiedClients = new ConcurrentDictionary<int, bool>();
        //enables concurrency
        _signal = new SemaphoreSlim(0);
    }

    public bool Push(TransactionRequest transactionRequest)
    {
        if (!OccupiedClients.TryAdd(transactionRequest.ClientId, true))
        {
            return false;
        }

        transactionRequest.DateTime = DateTime.Now;
        Queue.Enqueue(transactionRequest);
        _signal.Release();
        return true;
    }

    public TransactionRequest Remove()
    {
        if (Queue.TryDequeue(out var item))
        {
            OccupiedClients.TryRemove(item.ClientId, out _);
            return item;
        }

        return null;
    }

}
