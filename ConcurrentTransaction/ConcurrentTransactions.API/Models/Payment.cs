

using ConcurrentTransactions.API.Model;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

public class Payment
{
   // public Guid Id { get; set; }

    public int ClientId { get; set; } = default!;

    [Required(ErrorMessage = "Debtor Account is required.")]
    [StringLength(32, ErrorMessage = "Debtor Account exceed 32 characters.")]
    public string DebtorAccount { get; set; } = default!;
    [Required(ErrorMessage = "Creditor Account is required.")]
    [StringLength(32, ErrorMessage = "Creditor Account exceed 32 characters.")]
    public string CreditorAccount { get; set; } = default!;

    [Required]
    [RegularExpression(@"-?[0-9]{1,14}(\.[0-9]{1,3})?", ErrorMessage = "Invalid format for Amount.")]
    public required string InstructedAmount { get; set; }
    //public double Amount { get; set; } = default!;
    [AllowNull]
    public DateTime Timestamp { get; set; }
    [Required(ErrorMessage ="Currency Value is required")]
    public string Currency { get; set; } = default!;


    public TaskCompletionSource<Transaction> TransactionReceipt { get; set; } = default!;

}