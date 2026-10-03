using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WalletEntity = Wallet.Api.Models.Wallet;
namespace Wallet.Tests;
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"wallet-tests-{Guid.NewGuid()}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
     builder.UseSetting("ConnectionStrings:Wallet", $"Data Source={path};Pooling=False").UseEnvironment("Testing");
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && File.Exists(path)) File.Delete(path); }
}
public sealed class EndpointTests
{
    [Fact]
    public async Task RejectedHttpTransferDoesNotChangeBalancesOrHistory()
    {
        using var factory = new ApiFactory(); using var client = factory.CreateClient();
        var sourceResponse = await client.PostAsJsonAsync("/wallets", new { customerName = "Source", customerEmail = "source@example.com" });
        var targetResponse = await client.PostAsJsonAsync("/wallets", new { customerName = "Target", customerEmail = "target@example.com" });
        var source = (await sourceResponse.Content.ReadFromJsonAsync<WalletEntity>())!;
        var target = (await targetResponse.Content.ReadFromJsonAsync<WalletEntity>())!;
        var result = await client.PostAsJsonAsync("/wallets/transfer", new { fromWalletId = source.Id, toWalletId = target.Id, amount = 1m });
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(0m, (await client.GetFromJsonAsync<WalletEntity>($"/wallets/{source.Id}"))!.Balance);
        Assert.Equal(0m, (await client.GetFromJsonAsync<WalletEntity>($"/wallets/{target.Id}"))!.Balance);
        Assert.Equal("[]", await client.GetStringAsync($"/wallets/{source.Id}/transactions"));
        Assert.Equal("[]", await client.GetStringAsync($"/wallets/{target.Id}/transactions"));
    }
    [Fact]
    public async Task CompleteHttpJourney()
    {
        using var factory = new ApiFactory(); using var client = factory.CreateClient();
        async Task<WalletEntity> Create(string email)
        {
            var response = await client.PostAsJsonAsync("/wallets", new { customerName = "Alex", customerEmail = email });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            return (await response.Content.ReadFromJsonAsync<WalletEntity>())!;
        }
        var a = await Create("a@example.com"); var b = await Create("b@example.com");
        (await client.PostAsJsonAsync($"/wallets/{a.Id}/deposit", new { amount = 20.50m })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/wallets/transfer", new { fromWalletId = a.Id, toWalletId = b.Id, amount = 10m })).EnsureSuccessStatusCode();
        var source = await client.GetFromJsonAsync<WalletEntity>($"/wallets/{a.Id}");
        Assert.Equal(10.50m, source!.Balance);
        Assert.Equal(DateTimeKind.Utc, source.CreatedAt.Kind);
        var balance = await client.GetFromJsonAsync<Balance>($"/wallets/{b.Id}/balance");
        Assert.Equal(10m, balance!.Value);
        var history = await client.GetStringAsync($"/wallets/{b.Id}/transactions");
        Assert.Contains("TransferReceived", history); Assert.Contains("Completed", history);
        using var json = System.Text.Json.JsonDocument.Parse(history);
        Assert.Equal(DateTimeKind.Utc, json.RootElement[0].GetProperty("createdAt").GetDateTime().Kind);
    }
    private sealed record Balance([property: System.Text.Json.Serialization.JsonPropertyName("balance")] decimal Value);
    [Theory]
    [InlineData("{\"amount\":0}")]
    [InlineData("{\"amount\":-5}")]
    [InlineData("{\"amount\":0.001}")]
    [InlineData("{\"amount\":\"NaN\"}")]
    [InlineData("{\"amount\":1e999}")]
    [InlineData("{\"amount\":\"Infinity\"}")]
    public async Task InvalidJsonMoneyReturns400(string json)
    {
        using var factory = new ApiFactory(); using var client = factory.CreateClient();
        var result = await client.PostAsync($"/wallets/{Guid.NewGuid()}/deposit", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
    [Fact]
    public async Task MissingWalletAndDuplicateEmailUseConsistentErrors()
    {
        using var factory = new ApiFactory(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/wallets/{Guid.NewGuid()}")).StatusCode);
        var payload = new { customerName = "Alex", customerEmail = "a@example.com" };
        (await client.PostAsJsonAsync("/wallets", payload)).EnsureSuccessStatusCode();
        var duplicate = await client.PostAsJsonAsync("/wallets", payload);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.DoesNotContain("stack", await duplicate.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
