using Microsoft.AspNetCore.Mvc;

namespace Viw.WebApi.Controllers;

/// <summary>Convenções HTTP compartilhadas pelas consultas de leitura.</summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public abstract class ReadControllerBase : ControllerBase
{
    /// <summary>Retorna 200 para o recurso encontrado ou 404 quando ele não existe.</summary>
    /// <typeparam name="T">Contrato público da resposta.</typeparam>
    /// <param name="result">Resultado da consulta, ou null quando não encontrado.</param>
    /// <returns>Resposta HTTP correspondente à existência do recurso.</returns>
    protected ActionResult<T> Found<T>(T? result) where T : class => result is null ? NotFound() : Ok(result);
}
