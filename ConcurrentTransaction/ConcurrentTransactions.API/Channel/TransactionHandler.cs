using ConcurrentTransactions.API.Model;
using System.Collections.Concurrent;

namespace ConcurrentTransactions.API.Channel;

public class TransactionHandler
{
    private ConcurrentDictionary<int, SemaphoreSlim> ClientLocks = new();
    private ConcurrentDictionary<string, SemaphoreSlim> AccountLocks = new(StringComparer.OrdinalIgnoreCase);
    private ConcurrentBag<Transaction> SuccessfulPayments = new();
    private readonly ILogger<TransactionHandler> _logger;
    private readonly TransactionTracker _tracker;
    public TransactionHandler(ILogger<TransactionHandler> logger, TransactionTracker tracker)
    {
        _logger = logger;
        _tracker = tracker;
    }
    /// <summary>
    /// Attempts to make a transaction by first securing semaphores for one client
    /// and two separate string accounts
    /// </summary>
    /// <returns> async Task bool which indicates that a transaction was successful</returns>
    private async Task<bool> TryMakeNewTransaction(Payment transactionRequest, CancellationToken cancellationToken = default)
    {
        List<Action> cleanupActions = new();
        try
        {
            await AcquireLockWithCleanup(
                () => TryAcquireClientLock(transactionRequest.ClientId, cancellationToken),
                () => ReleaseClientLock(transactionRequest.ClientId),
                $"Failed to acquire client lock for ClientId: {transactionRequest.ClientId}",
                cleanupActions);

            await AcquireLockWithCleanup(
                () => TryAcquireAccountLock(transactionRequest.DebtorAccount, cancellationToken),
                () => ReleaseAccountLock(transactionRequest.DebtorAccount),
                $"Failed to acquire tier-2 lock for DebtorAccount: {transactionRequest.DebtorAccount}",
                cleanupActions);

            await AcquireLockWithCleanup(
                () => TryAcquireAccountLock(transactionRequest.CreditorAccount, cancellationToken),
                () => ReleaseAccountLock(transactionRequest.CreditorAccount),
                $"Failed to acquire tier-3 lock for CreditorAccount: {transactionRequest.CreditorAccount}",
                cleanupActions);

            return await ExecuteTransaction(transactionRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transaction initiation for ClientId: {ClientId}", transactionRequest.ClientId);
            throw; 
        }
        finally
        {
            
            foreach (var cleanupAction in cleanupActions)
            {
                try
                {
                    cleanupAction();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during lock cleanup.");
                }
            }
        }
    }
    /// <summary>
    /// Attempts to secure a unique lock, decrementing it's sole PC counter
    /// After a semaphore is secured successfully, it adds a to-do task in the actionlist,
    /// </summary>
    /// <returns> Returns a task</returns>
    private async Task AcquireLockWithCleanup(
    Func<Task<bool>> tryAcquireLock,
    Action releaseLock,
    string errorMessage,
    List<Action> cleanupActions)
    {
        if (!await tryAcquireLock())
        {
            _logger.LogWarning(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        cleanupActions.Add(releaseLock);
    }

    /// <summary>
    /// A wrapper function for issuing a new transction, creates a taskcompletion source token
    /// which stores a 'receipt' of a committed transaction if it is successful
    /// </summary>
    /// <returns> Returns a task</returns>
    public async Task ProcessTransaction(Payment transactionRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transactionRequest.DebtorAccount) ||
                string.IsNullOrWhiteSpace(transactionRequest.CreditorAccount))
            {
                throw new ArgumentException("Debtor or Creditor account cannot be null or empty.");
            }
            
            transactionRequest.TransactionReceipt = new TaskCompletionSource<Transaction>();
            var success = await TryMakeNewTransaction(transactionRequest, cancellationToken);

            if (!success)
            {
                //TODO: Change this
                var invalidops = new InvalidOperationException("Transaction failed due to lock contention or validation error.");
                transactionRequest.TransactionReceipt.SetException(
                    invalidops
                );
                throw invalidops;
            }
        }
        catch (OperationCanceledException ex)
        {
            transactionRequest?.TransactionReceipt.SetException(ex);
            _logger.LogWarning("Transaction canceled for ClientId: {ClientId}", transactionRequest?.ClientId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            transactionRequest?.TransactionReceipt.SetException(ex);
            _logger.LogError(ex, "Unexpected error processing transaction for ClientId: {ClientId}", transactionRequest?.ClientId);
            throw;
        }
    }
    /// <summary>
    /// Creates a delay for two seconds and create's a transaction and stores it in
    /// a concurrent bag.
    /// </summary>
    /// <returns> Returns a bool task</returns>
    private async Task<bool> ExecuteTransaction(Payment transaction)
    {
        try
        {
            // Simulate transaction processing
            _logger.LogWarning("Transaction underway");
            _tracker.StartTransaction();
            await Task.Delay(2000);
            var payment = new Transaction
            {
                Id = Guid.NewGuid(),
                ClientId = transaction.ClientId,
                DebtorAccount = transaction.DebtorAccount,
                CreditorAccount = transaction.CreditorAccount,
                TransactionAmount = transaction.InstructedAmount,
                TimeStamp = DateTime.Now,
                Currency = transaction.Currency,
            };
            SuccessfulPayments.Add(payment);
            transaction.TransactionReceipt.SetResult(payment);
            _logger.LogWarning("Transaction completed");

            return true;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing transaction for ClientId: {ClientId}", transaction.ClientId);
            throw;
        }
        finally
        {
            _tracker.EndTransaction();  
        }
    }
    /// <summary>
    /// Gets or creates client lock. 
    /// calls AcquireLockSync 
    /// </summary>
    /// <returns> Returns a bool task</returns>
    private async Task<bool> TryAcquireClientLock(int clientId, CancellationToken cancellationToken)
    {
        var clientLock = ClientLocks.GetOrAdd(clientId, _ => new SemaphoreSlim(1, 1));
        return await AcquireLockAsync(clientLock, cancellationToken);
    }

    private async Task<bool> TryAcquireAccountLock(string accountKey, CancellationToken cancellationToken = default)
    {
        var accountLock = AccountLocks.GetOrAdd(accountKey, _ => new SemaphoreSlim(1, 1));
        return await AcquireLockAsync(accountLock, cancellationToken);
    }
    /// <summary>
    /// attempts to acquire a specific semaphore
    /// if the counter is already zero, then return false
    /// </summary>
    /// <returns> Returns a bool task</returns>
    private async Task<bool> AcquireLockAsync(SemaphoreSlim semaphore, CancellationToken cancellation)
    {
        try
        {
            return await semaphore.WaitAsync(0, cancellation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire lock.");
            return false;
        }
    }
    /// <summary>
    /// Releases the semaphore, restoring its counter to 1
    /// 
    /// </summary>
    ///
    private void ReleaseClientLock(int clientId)
    {
        if (ClientLocks.TryGetValue(clientId, out var clientLock))
        {
            try
            {
                clientLock.Release();
            }
            catch (SemaphoreFullException ex)
            {
                _logger.LogError(ex, "Release called too many times for ClientId: {ClientId}", clientId);
            }
        }
    }

    private void ReleaseAccountLock(string accountKey)
    {
        if (AccountLocks.TryGetValue(accountKey, out var accountLock))
        {
            try
            {
                accountLock.Release();
            }
            catch (SemaphoreFullException ex)
            {
                _logger.LogError(ex, "Release called too many times for account: {AccountKey}", accountKey);
            }
        }
    }

    public async Task<(DateTime SnapshotTime, List<Transaction> Transactions)> GetSnapshotOfConfirmedTransactions(string IBAN)
    {
        
        try
        {
            if (!AccountLocks.ContainsKey(IBAN))
            {
                throw new KeyNotFoundException("Account not found");
            }
            if (_tracker.CheckIfAnyTransactionUnderway())
            {
                throw new InvalidOperationException("A transaction is already underway. Please try again later");
            }
            if (!await TryAcquireAccountLock(IBAN))
            {
                throw new InvalidOperationException("A transaction with that account detail is underway. Please try again later");
            }
            try
            {
                var snapshotTime = DateTime.Now;
                var snapshot = SuccessfulPayments
                              .Where(transaction =>
                               string.Equals(transaction.DebtorAccount, IBAN, StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(transaction.CreditorAccount, IBAN, StringComparison.OrdinalIgnoreCase))
                               .ToList();

                return (snapshotTime,snapshot);
            }
            catch(Exception ex)
            {
                throw;
            }
            finally
            {
                ReleaseAccountLock(IBAN);
            }

        }
        catch(Exception ex)
        {
            throw;
        }
    } 

}