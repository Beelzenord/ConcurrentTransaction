using ConcurrentTransactions.API.Model;
using ConcurrentTransactions.API.Models;

namespace ConcurrentTransactions.API.Channel;

public class Channel
{
    private readonly ProducerBuffer _producerBuffer;
    private readonly ConsumerBuffer _consumerBuffer;

    public Channel(ProducerBuffer producer, ConsumerBuffer consumer)
    {
        _producerBuffer = producer;
        _consumerBuffer = consumer;
        _producerBuffer.OnItemEnqueued += ProcessQueue;

    }

    public async Task<TransactionPost> AddToBufferAndAwaitResult(TransactionRequest transactionrequest, CancellationToken cancellation = default)
    {
        var completionSource = new TaskCompletionSource<TransactionPost>();
        transactionrequest.TransactionReceipt = completionSource;
        _producerBuffer.Push(transactionrequest);
        return await completionSource.Task.WaitAsync(cancellation);
    }
    private async void ProcessQueue()
    {
        while (!_producerBuffer.Queue.IsEmpty)
        {
            //Should pause here before dequeue
           // Console.WriteLine($"Pausing Before processessing : {_producerBuffer.Peek()?.ClientId}");
            await Task.Delay(2000); // Simulate processing time
          //  Console.WriteLine($"Unpausing and now processessing : {_producerBuffer.Peek()?.ClientId}");
            var transaction = _producerBuffer.Remove(); // Now, remove from queue
            if (transaction == null) break;

            var postTransaction = new TransactionPost
            {
                ClientId = transaction.ClientId,
                Id = transaction.Id,
                CreditorAccount = transaction.CreditorAccount,
                DebtorAccount = transaction.DebtorAccount,
                Amount = transaction.Amount, // Example transformation logic
                Timestamp = DateTime.UtcNow
            };

            _consumerBuffer.Batches.Add(postTransaction);
            //attach callback object
            transaction.TransactionReceipt.SetResult(postTransaction);

            Console.WriteLine($"[Channel] Processed transaction for Id: {transaction.Id}");

        }
    }
}
