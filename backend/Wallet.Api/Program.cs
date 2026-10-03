using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wallet.Api.Data;
using Wallet.Api.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var connection = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("Wallet") ?? "Data Source=wallet.db;Default Timeout=10")
{ ForeignKeys = true };
if (connection.DataSource != ":memory:")
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(connection.DataSource))!);
builder.Services.AddDbContext<WalletDbContext>(o => o.UseSqlite(connection.ToString()));
builder.Services.AddScoped<WalletService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
 .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173", "http://localhost:8080"])
 .AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<WalletDbContext>().Database.EnsureCreated();
app.UseMiddleware<ErrorMiddleware>();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapFallbackToFile("index.html");
app.Run();
public partial class Program;
