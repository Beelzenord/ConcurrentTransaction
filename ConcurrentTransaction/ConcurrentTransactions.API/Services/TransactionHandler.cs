using ConcurrentTransactions.API.Model;
using System.Collections.Concurrent;

namespace ConcurrentTransactions.API.Channel;
/// <summary>
/// A singleton that authorizes and conducts creation of Transaction objects
/// and a support method of extracting post-transactions
/// </summary>
public class TransactionHandler
{
    private ConcurrentDictionary<int, bool> ClientLocks = new();
    private ConcurrentDictionary<string, bool> AccountLocks = new(StringComparer.OrdinalIgnoreCase);
    private ConcurrentBag<Transaction> SuccessfulPayments = new();
    private readonly ILogger<TransactionHandler> _logger;
    private readonly TransactionTracker _tracker;

    public TransactionHandler(ILogger<TransactionHandler> logger, TransactionTracker tracker)
    {
        _logger = logger;
        _tracker = tracker;
    }
    /// <summary>
    /// Attempts to undergo a new transaction by first 
    /// checking if there are existing key-value entries in
    /// the concurrent-dictionary. If there are already matching keys with the client or account
    /// an exception is thrown.
    /// </summary>
    /// <returns> Returns a task</returns>
    public async Task TryProcessNewTransaction(Payment transactionRequest, CancellationToken cancellationToken =
      default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transactionRequest.DebtorAccount) ||
              string.IsNullOrWhiteSpace(transactionRequest.CreditorAccount))
            {
                throw new ArgumentException("Debtor or Creditor account cannot be null or empty.");
            }

            // Attempt to lock the client and accounts
            if (!TryLock(transactionRequest.ClientId, transactionRequest.DebtorAccount, transactionRequest.CreditorAccount))
            {
                throw new InvalidOperationException("Transaction conflict detected. Please try again later.");
            }

            try
            {
                // Simulate transaction processing
                transactionRequest.TransactionReceipt = new TaskCompletionSource<Transaction>();
                var success = await ExecuteTransaction(transactionRequest);
                if (!success)
                {
                    throw new InvalidOperationException("Transaction failed.");
                }
            }
            finally
            {
                // Always release locks after transaction
                ReleaseLocks(transactionRequest.ClientId, transactionRequest.DebtorAccount, transactionRequest.CreditorAccount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transaction for ClientId: {ClientId}", transactionRequest.ClientId);
            throw;
        }
    }
    /// <summary>
    /// Tries to add client id, debtor and creditor strings to the shared ConcurrentDictionaries   
    /// </summary>
    /// <returns> Returns a bool</returns>
    private bool TryLock(int clientId, string debtorAccount, string creditorAccount)
    {
        return ClientLocks.TryAdd(clientId, true) &&
          AccountLocks.TryAdd(debtorAccount, true) &&
          AccountLocks.TryAdd(creditorAccount, true);
    }
    /// <summary>
    /// Tries to remove client id, debtor and creditor strings from the shared ConcurrentDictionaries   
    /// </summary>
    private void ReleaseLocks(int clientId, string debtorAccount, string creditorAccount)
    {
        ClientLocks.TryRemove(clientId, out _);
        AccountLocks.TryRemove(debtorAccount, out _);
        AccountLocks.TryRemove(creditorAccount, out _);
    }
    /// <summary>
    /// Performs a delay for two seconds. Creates a transaction object and adds that to the concurrentBag
    /// Also sets a 'RanToCompletionState' to the Payments TaskCompletionSource which will be returned to the user
    /// </summary>
    /// <returns> Returns a bool task</returns>
    private async Task<bool> ExecuteTransaction(Payment transaction)
    {
        try
        {
            _tracker.StartTransaction();
            await Task.Delay(2000); // Simulate processing delay

            var payment = new Transaction
            {
                Id = Guid.NewGuid(),
                ClientId = transaction.ClientId,
                DebtorAccount = transaction.DebtorAccount,
                CreditorAccount = transaction.CreditorAccount,
                TransactionAmount = transaction.InstructedAmount,
                TimeStamp = DateTime.Now,
                Currency = transaction.Currency.ToUpper(),
            };
            transaction.TransactionReceipt.SetResult(payment);
            SuccessfulPayments.Add(payment);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing transaction for ClientId: {ClientId}", transaction.ClientId);
            return false;
        }
        finally
        {
            _tracker.EndTransaction();
        }
    }
    /// <summary>
    /// Gets a list of completed transactions involving the IBAN string parameter
    /// regardless if it was used as a creditor or debtor account.
    /// It can only be used if the TransactionTracker _activeTransactionCount is 0,
    /// which indicates that no transactions are underway and also if no user has begun the initial state of adding 
    /// a string key. An exception is thrown if any of the two occurs.
    /// </summary>
    /// <returns> Returns a DateTime and List<Transaction> tuple</Transaction></returns>
    public (DateTime SnapshotTime, List<Transaction> Transactions) GetSnapshotOfConfirmedTransactions(string IBAN)
    {
        // use tracker to see if transaction is ungoing
        if (_tracker.CheckIfAnyTransactionUnderway())
        {
            throw new InvalidOperationException("A transaction is already underway. Please try again later.");
        }
        // check if a lock is already secured
        if (AccountLocks.ContainsKey(IBAN))
        {
            throw new InvalidOperationException($"A transaction is still ongoing for IBAN: {IBAN}. Please wait until it completes.");
        }
        var snapshotTime = DateTime.Now;
        var snapshot = SuccessfulPayments
          .Where(t => string.Equals(t.DebtorAccount, IBAN, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.CreditorAccount, IBAN, StringComparison.OrdinalIgnoreCase))
          .ToList();

        return (snapshotTime, snapshot);
    }

}