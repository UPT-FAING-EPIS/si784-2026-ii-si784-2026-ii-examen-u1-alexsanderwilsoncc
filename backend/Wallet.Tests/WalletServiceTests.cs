using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wallet.Api.Data;
using Wallet.Api.Dtos;
using Wallet.Api.Models;
using Wallet.Api.Services;
using Wallet.Api.Validation;
namespace Wallet.Tests;

public sealed class WalletServiceTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly WalletDbContext db;
    private readonly WalletService service;
    public WalletServiceTests()
    {
        connection.Open();
        db = new(new DbContextOptionsBuilder<WalletDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        service = new(db);
    }
    private Task<Wallet.Api.Models.Wallet> Create(string email = "alex@example.com") => service.Create(new("Alex", email));
    [Fact]
    public async Task CreateWalletNormalizesEmail()
    {
        var w = await service.Create(new(" Alex ", " ALEX@example.com "));
        Assert.Equal("Alex", w.CustomerName);
        Assert.Equal("alex@example.com", w.CustomerEmail);
        Assert.Equal(0m, w.Balance);
        Assert.NotEqual(Guid.Empty, w.Id);
    }
    [Fact]
    public async Task DuplicateEmailRejected()
    {
        await Create();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Create("ALEX@example.com"))).StatusCode);
    }
    [Theory]
    [InlineData("", "a@example.com")]
    [InlineData("A", "invalid")]
    public async Task InvalidCustomerRejected(string name, string email) =>
     await Assert.ThrowsAsync<BusinessException>(() => service.Create(new(name, email)));
    [Fact]
    public async Task DepositRecordsMovement()
    {
        var w = await Create();
        await service.Deposit(w.Id, 10.25m);
        Assert.Equal(10.25m, (await service.Get(w.Id)).Balance);
        var tx = Assert.Single(await service.History(w.Id));
        Assert.Equal(TransactionType.Deposit, tx.Type);
        Assert.Equal(TransactionStatus.Completed, tx.Status);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.001)]
    public async Task InvalidDepositHasNoEffect(decimal amount)
    {
        var w = await Create();
        await Assert.ThrowsAsync<BusinessException>(() => service.Deposit(w.Id, amount));
        Assert.Equal(0m, w.Balance);
        Assert.Empty(await service.History(w.Id));
    }
    [Fact]
    public async Task TransferConservesFundsAndCreatesBothMovements()
    {
        var source = await Create(); var target = await Create("target@example.com");
        await service.Deposit(source.Id, 100m);
        await service.Transfer(new(source.Id, target.Id, 25.10m));
        Assert.Equal(74.90m, source.Balance); Assert.Equal(25.10m, target.Balance);
        Assert.Equal(100m, source.Balance + target.Balance);
        Assert.Equal(TransactionType.TransferSent, (await service.History(source.Id))[0].Type);
        var received = Assert.Single(await service.History(target.Id));
        Assert.Equal(TransactionType.TransferReceived, received.Type);
        Assert.Equal(source.Id, received.RelatedWalletId);
    }
    [Fact]
    public async Task InsufficientBalanceLeavesBothWalletsUntouched()
    {
        var a = await Create(); var b = await Create("b@example.com");
        await Assert.ThrowsAsync<BusinessException>(() => service.Transfer(new(a.Id, b.Id, 1m)));
        Assert.Equal(0m, a.Balance + b.Balance); Assert.Empty(await service.History(a.Id));
        Assert.Empty(await service.History(b.Id));
    }
    [Fact]
    public async Task SelfTransferRejected()
    {
        var w = await Create();
        await Assert.ThrowsAsync<BusinessException>(() => service.Transfer(new(w.Id, w.Id, 1m)));
    }
    [Fact]
    public async Task MissingWalletReturns404() =>
     Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => service.Get(Guid.NewGuid()))).StatusCode);
    [Fact]
    public async Task MissingRecipientDoesNotDebit()
    {
        var w = await Create(); await service.Deposit(w.Id, 5m);
        await Assert.ThrowsAsync<BusinessException>(() => service.Transfer(new(w.Id, Guid.NewGuid(), 2m)));
        Assert.Equal(5m, w.Balance); Assert.Single(await service.History(w.Id));
    }
    [Fact]
    public async Task BalanceLimitRejected()
    {
        var w = await Create(); await service.Deposit(w.Id, MoneyAttribute.Maximum);
        await Assert.ThrowsAsync<BusinessException>(() => service.Deposit(w.Id, 0.01m));
        Assert.Equal(MoneyAttribute.Maximum, w.Balance);
    }
    [Fact]
    public async Task DatabaseConstraintRollsBackTransfer()
    {
        var a = await Create(); var b = await Create("b@example.com");
        await service.Deposit(a.Id, 10m);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_received BEFORE INSERT ON Transactions WHEN NEW.Type = 'TransferReceived' BEGIN SELECT RAISE(ABORT, 'injected failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => service.Transfer(new(a.Id, b.Id, 2m)));
        db.ChangeTracker.Clear();
        Assert.Equal(10m, (await service.Get(a.Id)).Balance);
        Assert.Equal(0m, (await service.Get(b.Id)).Balance);
        Assert.Single(await service.History(a.Id)); Assert.Empty(await service.History(b.Id));
    }
    [Fact]
    public async Task StaleUpdateCannotOverwriteBalance()
    {
        var w = await Create();
        using var other = new WalletDbContext(new DbContextOptionsBuilder<WalletDbContext>().UseSqlite(connection).Options);
        var stale = await other.Wallets.SingleAsync();
        await service.Deposit(w.Id, 5m);
        stale.Balance = 3m; stale.Version = Guid.NewGuid();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => other.SaveChangesAsync());
        Assert.Equal(5m, (await service.Get(w.Id)).Balance);
    }
    public void Dispose() { db.Dispose(); connection.Dispose(); }
}
