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
    }
}
