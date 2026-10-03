using System.ComponentModel.DataAnnotations;
namespace Wallet.Api.Validation;
public sealed class MoneyAttribute : ValidationAttribute
{
    public const decimal Maximum = 1_000_000_000_000m;
    public MoneyAttribute() => ErrorMessage = "Use un importe positivo, máximo 1000000000000 y hasta dos decimales.";
    public override bool IsValid(object? value) => value is decimal amount && Valid(amount);
    public static bool Valid(decimal amount) => amount > 0 && amount <= Maximum && decimal.Round(amount, 2) == amount;
}