using ConcurrentTransactions.API.Model;
using System.Collections.Concurrent;

namespace ConcurrentTransactions.API.Channel
{
    public class SemaphorHandler
    {
        private ConcurrentDictionary<int, SemaphoreSlim> ClientLocks = new();
        private ConcurrentDictionary<string, SemaphoreSlim> AccountLocks = new(StringComparer.OrdinalIgnoreCase);
        private ConcurrentBag<Transaction> SuccessfulPayments = new();
        private ConcurrentQueue<int> test;
        
        private readonly ILogger<SemaphorHandler> _logger;

        public SemaphorHandler(ILogger<SemaphorHandler> logger)
        {
            _logger = logger;
            test = new ConcurrentQueue<int>();
            
        }

        public async Task<bool> TrySecureAccess(Payment transactionRequest, List<Action> cleanupActions, CancellationToken cancellationToken = default)
        {
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
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        
        public void PerformCleanupAction(List<Action> cleanupActions)
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

        /// <summary>
        /// Attempts to secure a unique lock, decrementing it's sole PC counter
        /// After a semaphore is secured successfully, it adds a to-do task in the actionlist,
        /// </summary>
        /// <returns> Returns a task</returns>
        private async Task AcquireLockWithCleanup(Func<Task<bool>> tryAcquireLock, Action releaseLock, string errorMessage,
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
        public async Task<bool> TryAccessAccount(string accountKey, CancellationToken cancellationToken = default)
        {
            return await TryAcquireAccountLock(accountKey, cancellationToken);
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
        public void StopLookingAtAccount(string accountKey)
        {
            //TODO:: try-catch
            ReleaseAccountLock(accountKey);
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
        public bool ContainsAccount(string accountKey)
        {
           return AccountLocks.ContainsKey(accountKey);
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
    }
}
