namespace ConcurrentTransactions.API.Models;

public class TransactionPost
{
    public int ClientId { get; set; } = default!;
    public string DebtorAccount { get; set; } = default;
    public string CreditorAccount { get; set; } = default!;
    public double Amount { get; set; } = default!;
}