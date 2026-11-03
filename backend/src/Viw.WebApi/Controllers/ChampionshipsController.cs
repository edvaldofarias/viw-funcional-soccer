using Microsoft.AspNetCore.Mvc;
using Viw.Application.Queries;
using Viw.Contracts.Catalog;

namespace Viw.WebApi.Controllers;

/// <summary>Consultas públicas de edições, equipes, partidas, estatísticas e premiações.</summary>
/// <param name="queries">Consultas de leitura da aplicação.</param>
[Route("api/v1/championships")]
public sealed class ChampionshipsController(IViwQueries queries) : ReadControllerBase
{
    /// <summary>Exibe classificação oficial por fase, jogos e campanha acumulada.</summary>
    /// <remarks>A posição da fase e os pontos vêm da fonte; posição geral em copas com mata-mata representa a fase atingida, com empates de colocação preservados. Não inventa decisões por pênaltis.</remarks>
    /// <param name="slug">Slug da edição.</param>
    /// <param name="ct">Cancelamento da consulta.</param>
    /// <returns>Fases, jogos e campanhas das equipes.</returns>
    [HttpGet("{slug}/overview")]
    [ProducesResponseType<CompetitionViewResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CompetitionViewResponse>> Overview(string slug,CancellationToken ct) => Found(await queries.CompetitionViewAsync(slug,ct));

    /// <summary>Lista as edições dos campeonatos.</summary>
    /// <remarks>Permite filtrar por ano. Ordena por ano e semestre decrescentes, com desempate pelo identificador. Não consulta a fonte externa.</remarks>
    /// <param name="request">Ano opcional e paginação; page inicia em 1 e pageSize aceita até 100 registros.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de edições e quantidade total encontrada.</returns>
    /// <response code="200">Página de edições e quantidade total encontrada. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet]
    [ProducesResponseType<PageResponse<EditionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<EditionResponse>>> List([FromQuery] EditionRequest request, CancellationToken ct) =>
        Ok(await queries.EditionsAsync(request, ct));

    /// <summary>Consulta os detalhes de uma edição.</summary>
    /// <remarks>Retorna fases, rodadas e contagens de equipes e partidas. O slug identifica a edição, não o campeonato externo.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="ct">Token de cancelamento da requisição; não é um parâmetro HTTP.</param>
    /// <returns>Edição, fases, rodadas e contagens.</returns>
    /// <response code="200">Edição, fases, rodadas e contagens.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}")]
    [ProducesResponseType<EditionDetailResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EditionDetailResponse>> Detail(string slug, CancellationToken ct) => Found(await queries.EditionAsync(slug, ct));

    /// <summary>Lista as equipes participantes de uma edição.</summary>
    /// <remarks>Ordena por nome e identificador. Retorna uma lista vazia quando a edição existe, mas ainda não tem equipes.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="request">Paginação: page entre 1 e 100000; pageSize entre 1 e 100; padrões 1 e 20.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de equipes da edição.</returns>
    /// <response code="200">Página de equipes da edição. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/teams")]
    [ProducesResponseType<PageResponse<TeamResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<TeamResponse>>> Teams(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.TeamsAsync(slug, request, ct));

    /// <summary>Consulta uma equipe e suas designações de técnicos.</summary>
    /// <remarks>A equipe deve pertencer à edição informada. Designações incluem período e eventual rodada; não indicam um único técnico atual.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="teamId">GUID interno da equipe nesta edição; não é o ID do Firebase.</param>
    /// <param name="ct">Token de cancelamento da requisição; não é um parâmetro HTTP.</param>
    /// <returns>Equipe e designações de técnicos cadastradas.</returns>
    /// <response code="200">Equipe e designações de técnicos cadastradas.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/teams/{teamId:guid}")]
    [ProducesResponseType<TeamDetailResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDetailResponse>> Team(string slug, Guid teamId, CancellationToken ct) => Found(await queries.TeamAsync(slug, teamId, ct));

    /// <summary>Detalha um jogador exclusivamente nesta edição, sem depender de identidade na Wiki.</summary>
    /// <remarks>Valida a inscrição na equipe. Totais e artilharia incluem todas as equipes da edição, apenas partidas encerradas; empates na artilharia são preservados. Em edição arquivada, todos os líderes em gols recebem reconhecimento de artilheiro. Colocação final só é retornada em edição arquivada. Não calcula MVP.</remarks>
    /// <param name="slug">Slug da edição.</param>
    /// <param name="teamId">Equipe do elenco consultado.</param>
    /// <param name="sourcePlayerId">ID do cadastro original da edição, retornado pelo elenco.</param>
    /// <param name="ct">Cancelamento da consulta.</param>
    /// <returns>Perfil da edição, estatísticas, artilharia, colocação da equipe e reconhecimentos editoriais ou automáticos.</returns>
    /// <response code="200">Dados do cadastro original restritos à edição consultada.</response>
    /// <response code="400">Identificadores inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Edição, equipe ou cadastro inexistente, ou cadastro não inscrito na equipe.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/teams/{teamId:guid}/players/{sourcePlayerId:guid}")]
    [ProducesResponseType<EditionPlayerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EditionPlayerResponse>> EditionPlayer(string slug, Guid teamId, Guid sourcePlayerId, CancellationToken ct) =>
        Found(await queries.EditionPlayerAsync(slug, teamId, sourcePlayerId, ct));

    /// <summary>Lista as inscrições de jogadores observadas em uma equipe.</summary>
    /// <remarks>Inclui inscrições abertas e encerradas, nomes originais e identidade oficial. Os períodos são observações do importador, não datas históricas exatas de transferência.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="teamId">GUID interno da equipe nesta edição; não é o ID do Firebase.</param>
    /// <param name="request">Paginação: page entre 1 e 100000; pageSize entre 1 e 100; padrões 1 e 20.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de inscrições vinculadas às identidades ativas.</returns>
    /// <response code="200">Página de inscrições vinculadas às identidades ativas. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/teams/{teamId:guid}/players")]
    [ProducesResponseType<PageResponse<RosterPlayerResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<RosterPlayerResponse>>> Roster(string slug, Guid teamId, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.RosterAsync(slug, teamId, request, ct));

    /// <summary>Lista as partidas de uma edição.</summary>
    /// <remarks>Aceita filtros de equipe, fase, rodada e estado. Ordena por data e identificador, deixando partidas sem data ao final. Placar desconhecido permanece nulo.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="request">Paginação e filtros opcionais de equipe, fase, rodada e estado da partida.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de partidas e quantidade total após os filtros.</returns>
    /// <response code="200">Página de partidas e quantidade total após os filtros. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/matches")]
    [ProducesResponseType<PageResponse<MatchResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<MatchResponse>>> Matches(string slug, [FromQuery] MatchRequest request, CancellationToken ct) =>
        Found(await queries.MatchesAsync(slug, request, ct));

    /// <summary>Consulta o placar e os dados de uma partida.</summary>
    /// <remarks>A partida deve pertencer à edição. Equipes ainda indefinidas e placares desconhecidos são retornados como null.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="matchId">GUID interno da partida nesta edição; não é o ID do Firebase.</param>
    /// <param name="ct">Token de cancelamento da requisição; não é um parâmetro HTTP.</param>
    /// <returns>Partida, participantes conhecidos, estado, placar e revisão.</returns>
    /// <response code="200">Partida, participantes conhecidos, estado, placar e revisão.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/matches/{matchId:guid}")]
    [ProducesResponseType<MatchResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchResponse>> Match(string slug, Guid matchId, CancellationToken ct) => Found(await queries.MatchAsync(slug, matchId, ct));

    /// <summary>Lista as estatísticas individuais registradas na súmula.</summary>
    /// <remarks>Preserva valores desconhecidos como null. Uma súmula com revisão anterior à partida é sinalizada como incompleta. Pode incluir dados de uma partida em andamento.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="matchId">GUID interno da partida nesta edição; não é o ID do Firebase.</param>
    /// <param name="request">Paginação: page entre 1 e 100000; pageSize entre 1 e 100; padrões 1 e 20.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de gols, cartões e presenças registrados por jogador.</returns>
    /// <response code="200">Página de gols, cartões e presenças registrados por jogador. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/matches/{matchId:guid}/players")]
    [ProducesResponseType<PageResponse<MatchPlayerResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<MatchPlayerResponse>>> MatchPlayers(string slug, Guid matchId, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.MatchPlayersAsync(slug, matchId, request, ct));

    /// <summary>Consulta os totais individuais de uma edição.</summary>
    /// <remarks>Considera apenas partidas encerradas e vínculos de identidade ativos. Ordena por gols, nome e identificador. Em edições encerradas, todos os líderes empatados em gols recebem o reconhecimento de artilheiro; presenças desconhecidas e súmulas incompletas são informadas separadamente.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="request">Paginação: page entre 1 e 100000; pageSize entre 1 e 100; padrões 1 e 20.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de identidades canônicas com seus totais registrados.</returns>
    /// <response code="200">Página de identidades canônicas com seus totais registrados. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/statistics/players")]
    [ProducesResponseType<PageResponse<PlayerStatisticsResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<PlayerStatisticsResponse>>> PlayerStatistics(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.PlayerStatisticsAsync(slug, request, ct));

    /// <summary>Consulta os resultados acumulados das equipes.</summary>
    /// <remarks>Considera partidas encerradas com placares válidos. Retorna jogos, vitórias, empates, derrotas e gols em ordem alfabética. Não atribui pontos, posições ou critérios de desempate.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="request">Paginação: page entre 1 e 100000; pageSize entre 1 e 100; padrões 1 e 20.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de totais por equipe, sem classificação por pontos.</returns>
    /// <response code="200">Página de totais por equipe, sem classificação por pontos. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/statistics/teams")]
    [ProducesResponseType<PageResponse<TeamStatisticsResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<TeamStatisticsResponse>>> TeamStatistics(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.TeamStatisticsAsync(slug, request, ct));

    /// <summary>Lista premiações confirmadas e reconhecimentos automáticos de artilharia.</summary>
    /// <remarks>Em edição encerrada, reconhece todos os líderes empatados com gols registrados, sem duplicar concessões editoriais válidas. As demais categorias exigem confirmação. Rascunhos e concessões revogadas não são publicados. Pagina primeiro as concessões editoriais por data e ID, depois a artilharia por nome e ID do cadastro.</remarks>
    /// <param name="slug">Slug da edição, por exemplo la-liga-2026-2.</param>
    /// <param name="request">Paginação: page entre 1 e 100000; pageSize entre 1 e 100; padrões 1 e 20.</param>
    /// <param name="ct">Token de cancelamento associado à requisição; não é um parâmetro HTTP.</param>
    /// <returns>Página de premiações confirmadas e seus destinatários.</returns>
    /// <response code="200">Página de premiações confirmadas e seus destinatários. Listas podem estar vazias.</response>
    /// <response code="400">Parâmetros inválidos; retorna ValidationProblemDetails.</response>
    /// <response code="404">Recurso inexistente ou não pertencente à edição solicitada; retorna ProblemDetails.</response>
    /// <response code="503">Banco não configurado ou indisponível; retorna ProblemDetails sem detalhes internos.</response>
    /// <response code="500">Falha inesperada; retorna ProblemDetails sem detalhes internos.</response>
    [HttpGet("{slug}/awards")]
    [ProducesResponseType<PageResponse<AwardResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<AwardResponse>>> Awards(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.AwardsAsync(slug, request, ct));
}
