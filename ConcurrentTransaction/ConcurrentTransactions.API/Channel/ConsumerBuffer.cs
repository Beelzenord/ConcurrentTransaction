using ConcurrentTransactions.API.Models;
using System.Collections.Concurrent;

namespace ConcurrentTransactions.API.Channel;

public class ConsumerBuffer
{
    public ConcurrentBag<TransactionPost> Batches;

    public ConsumerBuffer()
    {
        Batches = new ConcurrentBag<TransactionPost>();
    }
    
    public IList<TransactionPost> GetAllTransactionWithIban(string IBAN)
    {
        if (string.IsNullOrWhiteSpace(IBAN))
            throw new ArgumentException("IBAN cannot be null or empty.");

        return Batches.Where(t => t.CreditorAccount.Equals(IBAN, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    
    public string ToString()
    {
        throw new NotImplementedException();
    }
}