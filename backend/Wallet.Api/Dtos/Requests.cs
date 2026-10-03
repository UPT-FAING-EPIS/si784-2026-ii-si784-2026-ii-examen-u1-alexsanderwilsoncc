using System.ComponentModel.DataAnnotations;
using Wallet.Api.Validation;
namespace Wallet.Api.Dtos;
public sealed record CreateWalletRequest(
 [Required, StringLength(100)] string CustomerName,
 [Required, EmailAddress, StringLength(254)] string CustomerEmail);
public sealed record DepositRequest([Money] decimal Amount);
public sealed record TransferRequest(Guid FromWalletId, Guid ToWalletId, [Money] decimal Amount);
