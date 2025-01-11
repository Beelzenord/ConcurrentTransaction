using ConcurrentTransactions.API.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Concurrent;
using System.Threading;

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

    private async Task<bool> InitiateNewTransaction(Payment transactionRequest, CancellationToken cancellationToken = default)
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
            var success = await InitiateNewTransaction(transactionRequest, cancellationToken);

            if (!success)
            {
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
    private async Task<bool> ExecuteTransaction(Payment transaction)
    {
        try
        {
            // Simulate transaction processing
            _logger.LogWarning("Transaction underway");
            _tracker.StartTransaction();
            await Task.Delay(7000);
            var payment = new Transaction
            {
                Id = Guid.NewGuid(),
                ClientId = transaction.ClientId,
                DebtorAccount = transaction.DebtorAccount,
                CreditorAccount = transaction.CreditorAccount,
                Amount = transaction.Amount,
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