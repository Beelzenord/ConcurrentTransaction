

using ConcurrentTransactions.API.Model;
using ConcurrentTransactions.API.Util;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
/// <summary>
/// Represents a transaction request object
/// Holds a TaskCompletionSource which stores the consumer side 
/// receipt of a transfer if it was successful
/// </summary>
public class Payment
{
    public int ClientId { get; set; } = default!;

    [Required(ErrorMessage = "Debtor Account is required.")]
    [StringLength(32, ErrorMessage = "Debtor Account exceed 32 characters.")]
    [Trimmed(enforceTrim: true)]
    public string DebtorAccount { get; set; } = default!;
    [Required(ErrorMessage = "Creditor Account is required.")]
    [StringLength(32, ErrorMessage = "Creditor Account exceed 32 characters.")]
    [Trimmed(enforceTrim: true)]
    public string CreditorAccount { get; set; } = default!;

    [Required]
    [RegularExpression(@"-?[0-9]{1,14}(\.[0-9]{1,3})?", ErrorMessage = "Invalid format for Amount.")]
    [Trimmed(enforceTrim: true)]
    public required string InstructedAmount { get; set; }
    [AllowNull]
    public DateTime Timestamp { get; set; }
    [Required(ErrorMessage = "Currency Value is required")]
    [StringLength(3, ErrorMessage = "Currency Value should be three characters")]
    [ISOCurrencyValidator(ErrorMessage = "Currency Value should be valid ISO Code")]
    public string Currency { get; set; } = default!;


    public TaskCompletionSource<Transaction> TransactionReceipt { get; set; } = default!;

}
