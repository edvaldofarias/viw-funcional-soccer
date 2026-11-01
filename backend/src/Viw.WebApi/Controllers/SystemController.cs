using Microsoft.AspNetCore.Mvc;
using Viw.Contracts.System;

namespace Viw.WebApi.Controllers;

/// <summary>Informações públicas sobre a versão funcional da API.</summary>
[ApiController]
[Route("api/v1/system")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class SystemController : ControllerBase
{
    /// <summary>Consulta o estágio funcional da API.</summary>
    /// <remarks>Não exige banco de dados nem consulta o Firebase. ImportsImplemented informa a disponibilidade da implementação, não que uma importação foi concluída com sucesso.</remarks>
    /// <returns>Nome do sistema, estágio funcional e indicação de implementação do importador.</returns>
    /// <response code="200">Nome do sistema, estágio funcional e indicação de implementação do importador.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet]
    [ProducesResponseType<SystemStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemStatusResponse> Get() =>
        new SystemStatusResponse("VIW Funcional Soccer", "read-api", true);
}
