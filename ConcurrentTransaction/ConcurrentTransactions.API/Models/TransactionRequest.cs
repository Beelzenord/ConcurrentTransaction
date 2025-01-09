using ConcurrentTransactions.API.Models;

namespace ConcurrentTransactions.API.Model;
public class TransactionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ClientId { get; set; } = default!;
    public string DebtorAccount { get; set; } = default!;
    public string CreditorAccount { get; set; } = default!;
    public double Amount { get; set; } = default!;
    public DateTime TimeStamp { get; set; } = default!;
    public TaskCompletionSource<TransactionPost> TransactionReceipt { get; set; }
}
