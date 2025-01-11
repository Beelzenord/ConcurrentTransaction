namespace ConcurrentTransactions.API.Validator
{
    public class PaymentRequestValidator 
    {
        public static bool Validate(Payment paymentRequest, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(paymentRequest.DebtorAccount))
            {
                errorMessage = "Debtor Account can not be empty";
                return false;
            }

            if (paymentRequest.DebtorAccount.Length > 32)
            {
                errorMessage = "Debtor Account  must be less than or equal to 32 characters.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(paymentRequest.CreditorAccount))
            {
                errorMessage = "Creditor Account can not be empty";
                return false;
            }

            if (paymentRequest.CreditorAccount.Length > 32)
            {
                errorMessage = "Debtor Account  must be less than or equal to 32 characters.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(paymentRequest.Currency))
            {
                errorMessage = "Currency field can not be empty";
                return false;
            }

           

            errorMessage = string.Empty;
            return true;
        }
    }
}
