using Microsoft.AspNetCore.Mvc;
using Viw.Application.Queries;
using Viw.Contracts.Catalog;

namespace Viw.WebApi.Controllers;

/// <summary>Consultas públicas de identidades e histórico dos jogadores.</summary>
/// <param name="queries">Consultas de leitura da aplicação.</param>
[Route("api/v1/players")]
public sealed class PlayersController(IViwQueries queries) : ReadControllerBase
{
    /// <summary>Busca as identidades canônicas dos jogadores.</summary>
    /// <remarks>A busca ignora acentos e diferenças entre maiúsculas/minúsculas. Considera partes do nome oficial, nomes originais e apelidos de vínculos ativos, incluindo iniciais como E. Santana e pequenas diferenças de escrita com Fastenshtein. Nomes completos exatos têm prioridade sobre partes exatas, iniciais e aproximações; nome e identificador desempatarão resultados. Filtros de edição são aplicados antes da comparação e da paginação. Vários cadastros de origem não duplicam a mesma identidade; sugestões não unem pessoas automaticamente.</remarks>
    /// <param name="request">Busca opcional de até 100 caracteres, edição opcional e paginação.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de identidades oficiais por relevância quando há busca; sem busca, por nome e identificador.</returns>
    /// <response code="200">Página de identidades oficiais por relevância quando há busca; sem busca, por nome e identificador. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet]
    [ProducesResponseType<PageResponse<PlayerResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<PlayerResponse>>> List([FromQuery] PlayerRequest request, CancellationToken ct) =>
        Ok(await queries.PlayersAsync(request, ct));

    /// <summary>Resolve uma identidade pelo identificador estável.</summary>
    /// <remarks>Permite ao portal obter o slug canônico atual de um jogador referenciado em partidas ou premiações, sem deduzir o slug a partir do GUID.</remarks>
    /// <param name="id">GUID interno da identidade canônica do jogador.</param>
    /// <param name="ct">Token de cancelamento da requisição.</param>
    /// <returns>Identidade pública e slug canônico atual.</returns>
    /// <response code="200">Identidade pública encontrada.</response>
    /// <response code="400">Identificador inválido.</response>
    /// <response code="404">Identidade não cadastrada.</response>
    /// <response code="503">Dados temporariamente indisponíveis.</response>
    /// <response code="500">Falha inesperada na consulta.</response>
    [HttpGet("by-id/{id:guid}")]
    [ProducesResponseType<PlayerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerResponse>> ById(Guid id, CancellationToken ct) => Found(await queries.PlayerByIdAsync(id, ct));

    /// <summary>Consulta a identidade, totais, recordes e prêmios individuais da carreira.</summary>
    /// <remarks>Recordes, jogos, resultados, copas disputadas e prêmios abrangem todos os vínculos ativos, sem depender da paginação do histórico. Jogos e resultados contam cada partida uma vez; copas contam edições distintas com inscrição, incluindo edições em andamento. Resultados usam apenas jogos com participação e placar conhecidos, sem pênaltis de desempate. Classificações finais usam somente edições encerradas. Sequências continuam entre copas, mas empates e presença desconhecida interrompem a sequência; datas insuficientes impedem calculá-las. Apenas prêmios confirmados são publicados.</remarks>
    /// <param name="slug">Slug estável da identidade canônica, por exemplo jogador seguido de seu identificador.</param>
    /// <param name="ct">Token de cancelamento da requisição; não é um parâmetro HTTP.</param>
    /// <returns>Identidade canônica e totais registrados da carreira.</returns>
    /// <response code="200">Identidade canônica e totais registrados da carreira.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}")]
    [ProducesResponseType<PlayerDetailResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerDetailResponse>> Detail(string slug, CancellationToken ct) => Found(await queries.PlayerAsync(slug, ct));

    /// <summary>Lista as participações observadas do jogador entre as copas.</summary>
    /// <remarks>Usa vínculos de identidade ativos e preserva os nomes originais, equipes e edições. As datas descrevem o período observado pelo importador. FinalPosition usa a classificação geral final da equipe nas edições encerradas, compartilhando os critérios dos recordes; null indica campeonato não encerrado ou classificação desconhecida.</remarks>
    /// <param name="slug">Slug estável da identidade canônica, por exemplo jogador seguido de seu identificador.</param>
    /// <param name="request">Paginação: page entre 1 e 100000; pageSize entre 1 e 100; padrões 1 e 20.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de inscrições históricas da identidade canônica.</returns>
    /// <response code="200">Página de inscrições históricas da identidade canônica. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/history")]
    [ProducesResponseType<PageResponse<PlayerHistoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<PlayerHistoryResponse>>> History(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.PlayerHistoryAsync(slug, request, ct));
}
