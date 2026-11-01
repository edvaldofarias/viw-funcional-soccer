using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Filters;

public sealed class PortalResponseFilter(SeoService seo, ILogger<PortalResponseFilter> logger) : IAsyncActionFilter, IExceptionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = Error(context.Controller as Controller, context.HttpContext, 400, "Vamos conferir esses filtros?", "Um dos filtros não é válido. Confira os valores e tente novamente.");
            return;
        }
        await next();
    }
    public void OnException(ExceptionContext context)
    {
        var error = context.Exception;
        if (error is not (HttpRequestException or JsonException or InvalidDataException) &&
            !(error is OperationCanceledException && !context.HttpContext.RequestAborted.IsCancellationRequested)) return;
        logger.LogWarning(error, "Consulta do portal indisponível; rastreio {TraceId}.", context.HttpContext.TraceIdentifier);
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.Result = Error(null, context.HttpContext, 503, "Um intervalo rápido.", "Não conseguimos carregar os dados agora. Tente novamente em instantes.");
        context.ExceptionHandled = true;
    }
    private ViewResult Error(Controller? controller, HttpContext context, int status, string title, string message)
    {
        var data = controller?.ViewData ?? new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        data["Seo"] = seo.Create(title, message, context.Request.Path.Value ?? "/", true);
        context.Response.Headers.CacheControl = "no-store";
        return new ViewResult { StatusCode = status, ViewName = "PortalError", ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(data) { Model = new PortalError(status, title, message) } };
    }
}
