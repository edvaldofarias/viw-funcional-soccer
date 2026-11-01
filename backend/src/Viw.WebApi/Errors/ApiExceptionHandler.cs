using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Viw.Application.Queries;

namespace Viw.WebApi.Errors;

/// <summary>Converte falhas inesperadas das consultas em respostas ProblemDetails seguras.</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (context.RequestAborted.IsCancellationRequested) return false;
        var unavailable = exception is DataStoreUnavailableException or DbException;
        var status = unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
        logger.LogError(exception, "Falha na consulta da API; rastreio {TraceId}.", context.TraceIdentifier);
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-store";
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = new ProblemDetails {
            Status = status, Title = unavailable ? "Dados temporariamente indisponíveis." : "Não foi possível concluir a consulta."
        } });
        return true;
    }
}
