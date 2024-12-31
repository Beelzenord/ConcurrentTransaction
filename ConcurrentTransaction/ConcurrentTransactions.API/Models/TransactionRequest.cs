namespace ConcurrentTransactions.API.Model;
public class TransactionRequest
{
    public int ClientId { get; set; } = default!;
    public string DebtorAccount { get; set; } = default!;
    public string CreditorAccount { get; set; } = default!;
    public double Amount { get; set; } = default!;
    public DateTime DateTime { get; set; } = default!;
}
