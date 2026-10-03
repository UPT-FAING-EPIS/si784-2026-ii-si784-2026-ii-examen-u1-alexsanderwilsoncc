using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Dtos;
using Wallet.Api.Services;
namespace Wallet.Api.Controllers;
[ApiController]
[Route("wallets")]
public sealed class WalletsController(WalletService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateWalletRequest request)
    {
        var wallet = await service.Create(request);
        return CreatedAtAction(nameof(Get), new { id = wallet.Id }, wallet);
    }
    [HttpGet] public async Task<IActionResult> List() => Ok(await service.List());
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id) => Ok(await service.Get(id));
    [HttpGet("{id:guid}/balance")]
    public async Task<IActionResult> Balance(Guid id) => Ok(new { walletId = id, balance = (await service.Get(id)).Balance });
    [HttpPost("{id:guid}/deposit")]
    public async Task<IActionResult> Deposit(Guid id, DepositRequest request) => Ok(await service.Deposit(id, request.Amount));
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferRequest request)
    {
        await service.Transfer(request);
        return Ok(new { status = "Completed" });
    }
    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> History(Guid id) => Ok(await service.History(id));
}