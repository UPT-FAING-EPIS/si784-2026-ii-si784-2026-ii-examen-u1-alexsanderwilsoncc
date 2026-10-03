namespace Wallet.Api.Services;
public sealed class BusinessException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}