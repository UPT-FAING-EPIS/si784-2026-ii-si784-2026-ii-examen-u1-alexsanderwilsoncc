namespace Wallet.Api.Models;
public enum TransactionType { Deposit, TransferSent, TransferReceived }
public enum TransactionStatus { Completed, Rejected, Failed }
public sealed class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletId { get; set; }
    public Guid? RelatedWalletId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}