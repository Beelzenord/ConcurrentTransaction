using ConcurrentTransactions.API.Model;
using System.Collections.Concurrent;
using System.Xml;

namespace ConcurrentTransactions.API.Channel;
public class ProducerBuffer
{
    public ConcurrentDictionary<int, bool> OccupiedClients { get; }
    public ConcurrentQueue<TransactionRequest> Queue { get; }
    private readonly SemaphoreSlim _signal;
    public event Action OnItemEnqueued;
    public ProducerBuffer()
    {
        Queue = new ConcurrentQueue<TransactionRequest>();
        OccupiedClients = new ConcurrentDictionary<int, bool>();
        //enables concurrency
        _signal = new SemaphoreSlim(0);
        //TODO::Maybe add a buffer cap 
    }

    public bool Push(TransactionRequest transactionRequest)
    {
        if (!OccupiedClients.TryAdd(transactionRequest.ClientId, true))
        {
            return false;
        }

        transactionRequest.TimeStamp = DateTime.Now;
        Queue.Enqueue(transactionRequest);
        _signal.Release();
        OnItemEnqueued?.Invoke(); // Tell Channel that a queue can be consumed
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
    public bool IsIbanInPendingTransactionBeforeCommit(string creditorAccount)
    {
        return Queue.Any(t => t.CreditorAccount.Equals(creditorAccount, StringComparison.OrdinalIgnoreCase));
    }

    public void WaitForItem(CancellationToken cancellationToken = default)
    {
        _signal.Wait(cancellationToken);
    }

    public bool TryWaitForItem(TimeSpan timeout)
    {
        return _signal.Wait(timeout);
    }
}
