namespace ConcurrentTransactions.API.Channel
{
    /// <summary>
    /// A singleton observer that monitors the creation of a Transaction objects
    /// it's internal counter is incremented with every concurrent transaction
    /// and decremented when a transaction is finalized
    /// </summary>
    public class TransactionTracker
    {
        private int _activeTransactionCount = 0;

        public event Action? TransactionsStarted;
        public event Action? TransactionsEnded;

        /// <summary>
        /// Invoked when 'ExecuteTransaction' begins, which in turn invokes
        /// an atomic operation of incrementing _activeTransactionCount
        /// </summary>
        /// <returns></returns>
        public void StartTransaction()
        {
            int newCount = Interlocked.Increment(ref _activeTransactionCount);

            if (newCount == 1)
            {
                TransactionsStarted?.Invoke();
            }
        }
        /// <summary>
        /// Invoked when 'ExecuteTransaction' ends, and decrements the _activeTransactionCount
        /// in a thread safe manner
        /// </summary>
        /// <returns></returns>
        public void EndTransaction()
        {
            int newCount = Interlocked.Decrement(ref _activeTransactionCount);

            if (newCount == 0)
            {
                TransactionsEnded?.Invoke();
            }
        }
        /// <summary>
        /// Checks if _activeTransactionCount is greater than 0 
        /// </summary>
        /// <returns>a bool</returns>
        public bool CheckIfAnyTransactionUnderway()
        {
            return _activeTransactionCount > 0;
        }
        /// <summary>
        /// returns the value of _activeTransactionCount 
        /// </summary>
        /// <returns>a bool</returns>
        public int GetActiveTransactionCount()
        {
            return _activeTransactionCount;
        }
    }
}