namespace ConcurrentTransactions.API.Models;

public class TransactionPost
{
    public Guid Id { get; set; }
    public int ClientId { get; set; } = default!;
    public string DebtorAccount { get; set; } = default;
    public string CreditorAccount { get; set; } = default!;
    public double Amount { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}