 
namespace ConcurrentTransactions.API.Model;
public class Transaction
{
    public required Guid Id { get; set; } = Guid.NewGuid();
    public required int ClientId { get; set; } = default!;
    public required string DebtorAccount { get; set; } = default!;
    public required string CreditorAccount { get; set; } = default!;
    public required string TransactionAmount { get; set; } = default!;
    public DateTime TimeStamp { get; set; } = default!;
    public required string Currency { get; set; } 


}
public class SnapshotResponse
{
    public DateTime SnapshotTime { get; set; }
    public List<Transaction> Transactions { get; set; }
}