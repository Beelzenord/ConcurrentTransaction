namespace ConcurrentTransactions.API.Channel
{
    public class TransactionTracker
    {
        private int _activeTransactionCount = 0;

        public event Action? TransactionsStarted;
        public event Action? TransactionsEnded;

        public void StartTransaction()
        {
            int newCount = Interlocked.Increment(ref _activeTransactionCount);

            if (newCount == 1)
            {
                TransactionsStarted?.Invoke();
            }
        }

        public void EndTransaction()
        {
            int newCount = Interlocked.Decrement(ref _activeTransactionCount);

            if (newCount == 0)
            {
                TransactionsEnded?.Invoke();
            }
        }

        public bool CheckIfAnyTransactionUnderway()
        {
            return _activeTransactionCount > 0;
        }
        public int GetActiveTransactionCount()
        {
            return _activeTransactionCount;
        }


        /*

            if (!await TryAcquireClientLock(transactionRequest.ClientId, cancellationToken)) return false;

            try
            {
                if (!await TryAcquireAccountLock(transactionRequest.DebtorAccount, cancellationToken)) return false;

                try
                {
                    if (!await TryAcquireAccountLock(transactionRequest.CreditorAccount, cancellationToken)) return false;

                    try
                    {
                        await ExecuteTransaction(transactionRequest);
                    }
                    finally
                    {
                        ReleaseAccountLock(transactionRequest.CreditorAccount);
                    }
                }
                finally
                {
                    ReleaseAccountLock(transactionRequest.DebtorAccount);
                }
            }
            finally
            {
                ReleaseClientLock(transactionRequest.ClientId);
            }

            return true;
        
         
         
         
         List<Action> cleanupActions = new();
        try
        {
             // Acquire client lock
            if (!await TryAcquireClientLock(transactionRequest.ClientId, cancellationToken))
            {
                const string conflict = "$Faileds to acquire client lock for ClientId: {ClientId}";
                _logger.LogWarning("Faileds to acquire client lock for ClientId: {ClientId}", transactionRequest.ClientId);
                throw new InvalidOperationException(conflict);
            }
            cleanupActions.Add(() => ReleaseClientLock(transactionRequest.ClientId));

            // Acquire debtor account lock - tier 2
            if (!await TryAcquireAccountLock(transactionRequest.DebtorAccount, cancellationToken))
            {
                _logger.LogWarning("Failed to acquire lock for DebtorAccount: {DebtorAccount}", transactionRequest.DebtorAccount);
                throw new InvalidOperationException($"Failed to acquire tier-2 lock for DebtorAccount: {transactionRequest.DebtorAccount}");
            }
            cleanupActions.Add(() => ReleaseAccountLock(transactionRequest.DebtorAccount));

            // Acquire creditor account lock - tier 3
            if (!await TryAcquireAccountLock(transactionRequest.CreditorAccount, cancellationToken))
            {
                _logger.LogWarning($"Failed to acquire tier-3 lock for CreditorAccount: {transactionRequest.CreditorAccount}");
                throw new InvalidOperationException($"Failed to acquire tier-3 lock for CreditorAccount: {transactionRequest.CreditorAccount}");
            }
            cleanupActions.Add(() => ReleaseAccountLock(transactionRequest.CreditorAccount));

            // Execute the transaction
            if(await ExecuteTransaction(transactionRequest))
            {
                return true;

            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transaction initiation for ClientId: {ClientId}", transactionRequest.ClientId);
            throw; // Re-throw the exception after logging
        }
        finally
        {
            // Release all acquired locks
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
         
         
         
         
         
         
         
         */






    }
}
