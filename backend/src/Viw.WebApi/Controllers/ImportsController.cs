using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viw.Application.Queries;
using Viw.Contracts.Catalog;
using Viw.Contracts.Imports;
using Viw.WebApi.Security;

namespace Viw.WebApi.Controllers;

/// <summary>Monitoramento autenticado e somente de leitura das importações.</summary>
/// <param name="queries">Consultas de leitura da aplicação.</param>
[Route("api/v1/imports")]
[Authorize(Policy = MonitoringAuthenticationHandler.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
// A exploração da API considera os atributos deste controller em vez dos herdados.
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ImportsController(IViwQueries queries) : ReadControllerBase
{
    /// <summary>Lista as execuções de importação.</summary>
    /// <remarks>Exige X-Viw-Monitor-Key, chave exclusiva do servidor. HTTPS é obrigatório fora de Development. Aceita filtro por edição. Não inicia importações nem retorna snapshots ou segredos.</remarks>
    /// <param name="request">Edição opcional e paginação das execuções, ordenadas da mais recente para a mais antiga.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página com estados, horários e contagens das execuções.</returns>
    /// <response code="200">Página com estados, horários e contagens das execuções. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="401">Chave ausente, inválida, acesso desabilitado ou transporte inseguro.</response>
    /// <response code="403">Identidade autenticada sem a permissão de leitura de importações.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("runs")]
    [ProducesResponseType<PageResponse<ImportRunResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<ImportRunResponse>>> Runs([FromQuery] ImportRunRequest request, CancellationToken ct) =>
        Ok(await queries.ImportRunsAsync(request, ct));

    /// <summary>Consulta o estado de uma execução de importação.</summary>
    /// <remarks>Exige X-Viw-Monitor-Key. Informa a lease atual da edição e se uma execução em Running pode precisar de recuperação. A consulta não toma a lease nem altera a execução; não calcula uma porcentagem fictícia de progresso.</remarks>
    /// <param name="id">GUID interno da execução de importação.</param>
    /// <param name="ct">Token de cancelamento da requisição; não é um parâmetro HTTP.</param>
    /// <returns>Execução, contagens, horários e indicadores operacionais da lease.</returns>
    /// <response code="200">Execução, contagens, horários e indicadores operacionais da lease.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="401">Chave ausente, inválida, acesso desabilitado ou transporte inseguro.</response>
    /// <response code="403">Identidade autenticada sem a permissão de leitura de importações.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("runs/{id:guid}")]
    [ProducesResponseType<ImportRunDetailResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportRunDetailResponse>> Run(Guid id, CancellationToken ct) => Found(await queries.ImportRunAsync(id, ct));

    /// <summary>Lista os últimos checkpoints confirmados de uma edição.</summary>
    /// <remarks>Exige X-Viw-Monitor-Key. Os checkpoints representam o estado atual da edição, não o histórico de uma execução específica. Não retorna cursores, hashes, credenciais ou JSON de origem.</remarks>
    /// <param name="editionId">GUID interno da edição cujo estado atual será consultado.</param>
    /// <param name="request">Paginação: page entre 1 e 100000; pageSize entre 1 e 100; padrões 1 e 20.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página com nomes dos recursos confirmados e horários de atualização.</returns>
    /// <response code="200">Página com nomes dos recursos confirmados e horários de atualização. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="401">Chave ausente, inválida, acesso desabilitado ou transporte inseguro.</response>
    /// <response code="403">Identidade autenticada sem a permissão de leitura de importações.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("editions/{editionId:guid}/checkpoints")]
    [ProducesResponseType<PageResponse<ImportCheckpointResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<ImportCheckpointResponse>>> Checkpoints(Guid editionId, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.ImportCheckpointsAsync(editionId, request, ct));
}
