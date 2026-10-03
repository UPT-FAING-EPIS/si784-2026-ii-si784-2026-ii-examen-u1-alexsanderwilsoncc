using Microsoft.EntityFrameworkCore;
using Wallet.Api.Models;
using WalletEntity = Wallet.Api.Models.Wallet;
namespace Wallet.Api.Data;
public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options) : DbContext(options)
{
    public DbSet<WalletEntity> Wallets => Set<WalletEntity>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        var w = model.Entity<WalletEntity>();
        w.HasKey(x => x.Id);
        w.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
        w.Property(x => x.CustomerEmail).HasMaxLength(254).IsRequired();
        w.HasIndex(x => x.CustomerEmail).IsUnique();
        w.Property(x => x.Balance).HasConversion(x => (long)(x * 100m), x => x / 100m);
        w.Property(x => x.Version).IsConcurrencyToken();
        w.Property(x => x.CreatedAt).HasConversion(x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
        w.ToTable("Wallets", t =>
        {
            t.HasCheckConstraint("CK_Wallet_Balance", "Balance >= 0 AND Balance <= 100000000000000");
            t.HasCheckConstraint("CK_Wallet_Name", "length(CustomerName) BETWEEN 1 AND 100");
            t.HasCheckConstraint("CK_Wallet_Email", "length(CustomerEmail) BETWEEN 3 AND 254");
        });
        var tx = model.Entity<Transaction>();
        tx.HasKey(x => x.Id);
        tx.Property(x => x.Amount).HasConversion(x => (long)(x * 100m), x => x / 100m);
        tx.Property(x => x.Type).HasConversion<string>();
        tx.Property(x => x.Status).HasConversion<string>();
        tx.Property(x => x.CreatedAt).HasConversion(x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
        tx.HasOne<WalletEntity>().WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        tx.HasOne<WalletEntity>().WithMany().HasForeignKey(x => x.RelatedWalletId).OnDelete(DeleteBehavior.Restrict);
        tx.HasIndex(x => new { x.WalletId, x.CreatedAt });
        tx.ToTable("Transactions", t =>
        {
            t.HasCheckConstraint("CK_Transaction_Amount", "Amount > 0 AND Amount <= 100000000000000");
            t.HasCheckConstraint("CK_Transaction_Type", "Type IN ('Deposit','TransferSent','TransferReceived')");
            t.HasCheckConstraint("CK_Transaction_Status", "Status IN ('Completed','Rejected','Failed')");
            t.HasCheckConstraint("CK_Transaction_Related", "(Type = 'Deposit' AND RelatedWalletId IS NULL) OR (Type <> 'Deposit' AND RelatedWalletId IS NOT NULL AND RelatedWalletId <> WalletId)");
        });
    }
}
