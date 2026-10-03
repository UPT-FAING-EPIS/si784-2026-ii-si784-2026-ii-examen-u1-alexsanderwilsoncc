using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace Wallet.Api.Services;
public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                BusinessException business => (business.StatusCode, business.Message),
                DbUpdateConcurrencyException => (409, "El saldo cambió. Actualice e intente nuevamente."),
                DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 19 } } => (409, "Los datos entran en conflicto con un registro existente."),
                SqliteException { SqliteErrorCode: 5 or 6 } => (409, "Billetera ocupada. Intente nuevamente."),
                DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 5 or 6 } } => (409, "Billetera ocupada. Intente nuevamente."),
                _ => (500, "No se pudo completar la operación.")
            };
            if (status == 500) logger.LogError(exception, "Unhandled request failure {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Extensions = { ["traceId"] = context.TraceIdentifier } });
        }
    }
}