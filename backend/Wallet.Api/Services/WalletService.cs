using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Wallet.Api.Data;
using Wallet.Api.Dtos;
using Wallet.Api.Models;
using Wallet.Api.Validation;
using WalletEntity = Wallet.Api.Models.Wallet;
namespace Wallet.Api.Services;
public sealed class WalletService(WalletDbContext db)
{
    public async Task<WalletEntity> Create(CreateWalletRequest request)
    {
        var name = request.CustomerName?.Trim() ?? "";
        var email = request.CustomerEmail?.Trim().ToLowerInvariant() ?? "";
        if (name.Length is 0 or > 100 || email.Length is 0 or > 254 || !new EmailAddressAttribute().IsValid(email))
            throw new BusinessException(400, "Nombre y correo válido son obligatorios.");
        if (await db.Wallets.AnyAsync(x => x.CustomerEmail == email))
            throw new BusinessException(409, "El correo ya tiene una billetera.");
        var wallet = new WalletEntity { CustomerName = name, CustomerEmail = email };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }
    public async Task<WalletEntity> Get(Guid id) => await db.Wallets.FindAsync(id)
     ?? throw new BusinessException(404, "Billetera no encontrada.");
    public Task<List<WalletEntity>> List() => db.Wallets.AsNoTracking().OrderBy(x => x.CustomerName).ToListAsync();
    public async Task<List<Transaction>> History(Guid id)
    {
        await Get(id);
        return await db.Transactions.AsNoTracking().Where(x => x.WalletId == id)
         .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync();
    }
    public async Task<WalletEntity> Deposit(Guid id, decimal amount)
    {
        ValidateAmount(amount);
        await using var atomic = await db.Database.BeginTransactionAsync();
        var wallet = await Get(id);
        ValidateCapacity(wallet, amount);
        wallet.Balance += amount;
        wallet.Version = Guid.NewGuid();
        db.Transactions.Add(new Transaction { WalletId = id, Amount = amount, Type = TransactionType.Deposit });
        await db.SaveChangesAsync();
        await atomic.CommitAsync();
        return wallet;
    }
    public async Task Transfer(TransferRequest request)
    {
        ValidateAmount(request.Amount);
        if (request.FromWalletId == request.ToWalletId)
            throw new BusinessException(400, "Origen y destino deben ser distintos.");
        await using var atomic = await db.Database.BeginTransactionAsync();
        var source = await Get(request.FromWalletId);
        var target = await Get(request.ToWalletId);
        if (source.Balance < request.Amount) throw new BusinessException(400, "Saldo insuficiente.");
        ValidateCapacity(target, request.Amount);
        source.Balance -= request.Amount;
        target.Balance += request.Amount;
        source.Version = Guid.NewGuid();
        target.Version = Guid.NewGuid();
        var date = DateTime.UtcNow;
        db.Transactions.AddRange(
         new Transaction { WalletId = source.Id, RelatedWalletId = target.Id, Amount = request.Amount, Type = TransactionType.TransferSent, CreatedAt = date },
         new Transaction { WalletId = target.Id, RelatedWalletId = source.Id, Amount = request.Amount, Type = TransactionType.TransferReceived, CreatedAt = date });
        await db.SaveChangesAsync();
        await atomic.CommitAsync();
    }
    private static void ValidateAmount(decimal amount)
    {
        if (!MoneyAttribute.Valid(amount)) throw new BusinessException(400, "Importe inválido: use un valor positivo con hasta dos decimales.");
    }
    private static void ValidateCapacity(WalletEntity wallet, decimal amount)
    {
        if (wallet.Balance > MoneyAttribute.Maximum - amount)
            throw new BusinessException(400, "El saldo supera el máximo permitido.");
    }
}