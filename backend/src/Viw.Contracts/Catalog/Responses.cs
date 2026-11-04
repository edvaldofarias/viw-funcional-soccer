namespace Viw.Contracts.Catalog;

/// <summary>Página de resultados com metadados de paginação.</summary>
/// <typeparam name="T">Contrato dos registros retornados.</typeparam>
/// <param name="Items">Registros desta página, possivelmente vazios.</param>
/// <param name="Page">Número da página, iniciado em 1.</param>
/// <param name="PageSize">Limite de registros por página.</param>
/// <param name="TotalCount">Total de registros após aplicar os filtros, antes da paginação.</param>
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Identificação pública de uma edição do campeonato.</summary>
/// <param name="Id">GUID interno da edição.</param>
/// <param name="CompetitionId">GUID interno da competição à qual a edição pertence.</param>
/// <param name="Slug">Slug estável utilizado nas rotas da edição.</param>
/// <param name="Name">Nome da edição.</param>
/// <param name="Year">Ano da edição.</param>
/// <param name="Semester">Semestre: 1 ou 2.</param>
/// <param name="Status">Situação da edição: Draft, Active ou Archived.</param>
/// <param name="TimeZoneId">Identificador do fuso usado para apresentar as datas da edição.</param>
/// <param name="ImageAssetId">Referência da imagem no storage; null se ainda indisponível. Não é a URL do Firebase.</param>
/// <param name="CoverAssetId">Referência da capa no storage, quando disponível.</param>
public sealed record EditionResponse(Guid Id, Guid CompetitionId, string Slug, string Name, int Year,
    int Semester, string Status, string TimeZoneId, Guid? ImageAssetId, Guid? CoverAssetId);

/// <summary>Fase cadastrada na edição.</summary>
/// <param name="Id">GUID interno da fase.</param>
/// <param name="Name">Nome cadastrado da fase.</param>
/// <param name="SortOrder">Ordem de apresentação da fase.</param>
public sealed record PhaseResponse(Guid Id, string Name, int SortOrder);

/// <summary>Grupo editorial de uma fase (ex.: Grupo A), definido no catálogo da edição.</summary>
/// <param name="Id">GUID interno do grupo.</param>
/// <param name="PhaseId">GUID interno da fase que contém o grupo.</param>
/// <param name="Name">Nome cadastrado do grupo.</param>
/// <param name="SortOrder">Ordem de apresentação do grupo dentro da fase.</param>
public sealed record GroupResponse(Guid Id, Guid PhaseId, string Name, int SortOrder);

/// <summary>Rodada cadastrada na edição.</summary>
/// <param name="Id">GUID interno da rodada.</param>
/// <param name="PhaseId">GUID interno da fase que contém a rodada.</param>
/// <param name="Name">Nome cadastrado da rodada.</param>
/// <param name="Number">Número da rodada dentro da fase.</param>
public sealed record RoundResponse(Guid Id, Guid PhaseId, string Name, int Number);

/// <summary>Detalhes da edição, estrutura e contagens de registros.</summary>
/// <param name="Edition">Identificação da edição.</param>
/// <param name="Teams">Quantidade de equipes cadastradas nesta edição.</param>
/// <param name="Matches">Quantidade de partidas cadastradas nesta edição.</param>
/// <param name="Phases">Fases em ordem de apresentação.</param>
/// <param name="Rounds">Rodadas cadastradas nas fases.</param>
public sealed record EditionDetailResponse(EditionResponse Edition, int Teams, int Matches,
    IReadOnlyList<PhaseResponse> Phases, IReadOnlyList<RoundResponse> Rounds);

/// <summary>Equipe participante da edição.</summary>
/// <param name="Id">GUID interno da equipe participante, utilizado nas rotas desta edição.</param>
/// <param name="Name">Nome da equipe.</param>
/// <param name="ImageAssetId">Referência da imagem oficial ou da imagem importada; null quando indisponível.</param>
public sealed record TeamResponse(Guid Id, string Name, Guid? ImageAssetId);

/// <summary>Designação de um técnico, com período e eventual rodada.</summary>
/// <param name="Id">GUID interno da identidade do técnico.</param>
/// <param name="Slug">Slug estável do técnico.</param>
/// <param name="Name">Nome oficial do técnico.</param>
/// <param name="RoundId">Rodada específica da designação, ou null para um período sem rodada específica.</param>
/// <param name="StartsAt">Início do período de designação.</param>
/// <param name="EndsAt">Fim da designação; null enquanto não encerrada.</param>
public sealed record CoachResponse(Guid Id, string Slug, string Name, Guid? RoundId,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt);

/// <summary>Equipe e designações de técnicos cadastradas.</summary>
/// <param name="Team">Identificação da equipe.</param>
/// <param name="Coaches">Designações registradas; podem incluir períodos encerrados e rodadas específicas.</param>
public sealed record TeamDetailResponse(TeamResponse Team, IReadOnlyList<CoachResponse> Coaches);

/// <summary>Identidade canônica de um jogador da VIW.</summary>
/// <param name="Id">GUID interno da identidade canônica; diferente do ID do cadastro da fonte.</param>
/// <param name="Slug">Slug estável utilizado nas rotas do jogador.</param>
/// <param name="Name">Nome oficial da identidade, preservado pelo importador.</param>
/// <param name="ImageAssetId">Referência da foto oficial, ou da foto importada quando disponível.</param>
public sealed record PlayerResponse(Guid Id, string Slug, string Name, Guid? ImageAssetId);

/// <summary>Totais registrados; cartões de edições arquivadas usam os agregados quando disponíveis, sem somá-los às súmulas.</summary>
/// <param name="Goals">Soma dos gols conhecidos; em edições encerradas, líderes empatados recebem o reconhecimento de artilheiro.</param>
/// <param name="YellowCards">Soma dos cartões amarelos conhecidos.</param>
/// <param name="RedCards">Soma dos cartões vermelhos conhecidos.</param>
/// <param name="KnownAppearances">Soma das presenças explicitamente registradas.</param>
/// <param name="UnknownAppearances">Quantidade de registros em que a presença não pôde ser determinada; não equivale a ausência.</param>
/// <param name="IncompleteSheets">Quantidade de registros com súmula incompleta ou com revisão anterior à da partida.</param>
public sealed record PlayerTotalsResponse(long Goals, long YellowCards, long RedCards,
    long KnownAppearances, int UnknownAppearances, int IncompleteSheets);

/// <summary>Cartões do jogador na edição inteira, sem atribuir agregados a partidas ou equipes.</summary>
public sealed record PlayerDisciplineResponse(long YellowCards, long RedCards);

/// <summary>Identidade canônica e totais registrados da carreira.</summary>
/// <param name="Player">Identificação oficial do jogador.</param>
/// <param name="Statistics">Totais das edições vinculadas; sujeitos à disponibilidade das súmulas.</param>
/// <param name="Aliases">Nomes e apelidos originais dos cadastros associados.</param>
/// <param name="Records">Recordes de toda a carreira vinculada, independentes da paginação do histórico.</param>
/// <param name="Awards">Reconhecimentos editoriais confirmados e artilharia automática das edições encerradas de todos os cadastros vinculados.</param>
/// <param name="Results">Jogos e resultados de toda a carreira, contando cada partida uma vez.</param>
/// <param name="CupsPlayed">Quantidade de edições distintas com inscrição vinculada, incluindo campeonatos em andamento.</param>
/// <param name="Podium">Títulos, vices e terceiros nas classificações finais das edições encerradas, contando uma conquista por edição.</param>
public sealed record PlayerDetailResponse(PlayerResponse Player, PlayerTotalsResponse Statistics,
    IReadOnlyList<string>? Aliases = null, PlayerRecordsResponse? Records = null, IReadOnlyList<PlayerAwardResponse>? Awards = null,
    PlayerResultsResponse? Results = null, int? CupsPlayed = null, PlayerPodiumResponse? Podium = null);

/// <summary>Conquistas finais da carreira; cada edição encerrada conta pela melhor colocação entre as equipes vinculadas.</summary>
public sealed record PlayerPodiumResponse(int Titles, int RunnersUp, int ThirdPlaces);

/// <summary>Jogos confirmados e resultados da carreira, sem duplicar partidas entre cadastros vinculados.</summary>
/// <param name="Played">Jogos encerrados com participação confirmada, mesmo quando o placar é desconhecido.</param>
/// <param name="Wins">Vitórias nos jogos com placar conhecido, sem pênaltis de desempate.</param>
/// <param name="Draws">Empates nos jogos com placar conhecido, sem pênaltis de desempate.</param>
/// <param name="Losses">Derrotas nos jogos com placar conhecido, sem pênaltis de desempate.</param>
public sealed record PlayerResultsResponse(long Played, int Wins, int Draws, int Losses);

/// <summary>Totais de um jogador em uma edição específica.</summary>
/// <param name="Player">Identidade canônica usada para consolidar os cadastros da fonte.</param>
/// <param name="Statistics">Totais registrados nas partidas encerradas desta edição.</param>
public sealed record PlayerStatisticsResponse(PlayerResponse Player, PlayerTotalsResponse Statistics);

/// <remarks>Statistics e Results são restritos à edição/equipe desta inscrição. Results considera somente partidas com presença conhecida; pênaltis não são somados aos gols. ImageUrl preserva a foto original da edição.</remarks>
/// <summary>Inscrição observada da identidade canônica em uma edição e equipe.</summary>
/// <param name="Edition">Edição em que o cadastro participou.</param>
/// <param name="Team">Equipe vinculada à inscrição.</param>
/// <param name="OriginalName">Nome recebido da fonte nessa edição, preservado para conferência.</param>
/// <param name="ObservedFrom">Início observado pelo importador; não afirma a data histórica exata da transferência.</param>
/// <param name="ObservedUntil">Fim observado da inscrição; null enquanto a inscrição estiver aberta.</param>
/// <param name="ImageUrl">Foto original desta edição, quando disponível.</param>
/// <param name="Statistics">Estatísticas individuais restritas à edição e equipe desta inscrição.</param>
/// <param name="Results">Resultados nos jogos com presença conhecida do jogador.</param>
/// <param name="Discipline">Cartões da edição inteira; repetidos nas inscrições quando há mais de uma equipe na mesma edição.</param>
/// <param name="FinalPosition">Classificação geral final da equipe em uma edição encerrada; null se ainda não encerrada ou desconhecida.</param>
public sealed record PlayerHistoryResponse(EditionResponse Edition, TeamResponse Team, string OriginalName,
    DateTimeOffset ObservedFrom, DateTimeOffset? ObservedUntil,
    string? ImageUrl = null, PlayerTotalsResponse? Statistics = null, TeamStatisticsResponse? Results = null,
    PlayerDisciplineResponse? Discipline = null, int? FinalPosition = null);

/// <summary>Jogador e dados da inscrição observada na equipe.</summary>
/// <param name="Player">Cadastro original da edição; o slug aponta para a Wiki quando há identidade vinculada.</param>
/// <param name="OriginalName">Nome do cadastro original nesta edição.</param>
/// <param name="ShirtNumber">Número da camisa recebido da fonte; null se não informado.</param>
/// <param name="Position">Posição recebida da fonte; null se não informada.</param>
/// <param name="ObservedFrom">Início observado da inscrição, sem garantia de data histórica exata.</param>
/// <param name="ObservedUntil">Fim observado da inscrição; null enquanto aberta.</param>
/// <param name="Statistics">Estatísticas nas partidas encerradas desta equipe e edição.</param>
/// <param name="Discipline">Cartões da edição inteira, seguindo a mesma regra da Wiki e da classificação individual.</param>
public sealed record RosterPlayerResponse(PlayerResponse Player, string OriginalName, string? ShirtNumber,
    string? Position, DateTimeOffset ObservedFrom, DateTimeOffset? ObservedUntil,
    PlayerTotalsResponse? Statistics = null, PlayerDisciplineResponse? Discipline = null);

/// <summary>Detalhes do cadastro original nesta edição, com link opcional para a Wiki vinculada.</summary>
/// <param name="Edition">Edição consultada.</param>
/// <param name="Team">Equipe do contexto consultado ou vínculo observado mais recente; null quando não há equipe conhecida.</param>
/// <param name="Player">Cadastro original; ID da fonte, nome da edição e slug vazio.</param>
/// <param name="ShirtNumber">Camisa informada pela fonte.</param>
/// <param name="Position">Posição informada pela fonte.</param>
/// <param name="Statistics">Totais da edição inteira, incluindo todas as equipes; somente partidas encerradas.</param>
/// <param name="Results">Resultados nas partidas com presença confirmada na edição.</param>
/// <param name="ScoringPosition">Posição entre totais distintos de gols da edição, sem saltos após empates (1, 1, 2); null sem gols registrados.</param>
/// <param name="FinalPosition">Colocação geral da equipe, somente em edição arquivada e quando conhecida.</param>
/// <param name="Awards">Reconhecimentos editoriais confirmados e artilharia automática deste cadastro na edição encerrada.</param>
/// <param name="WikiSlug">Slug canônico do vínculo ativo, apenas para navegação opcional à carreira; null sem vínculo.</param>
public sealed record EditionPlayerResponse(EditionResponse Edition, TeamResponse? Team, PlayerResponse Player,
    string? ShirtNumber, string? Position, PlayerTotalsResponse Statistics, PlayerResultsResponse Results,
    int? ScoringPosition, int? FinalPosition, IReadOnlyList<AwardResponse> Awards, string? WikiSlug = null);

/// <summary>Partida com participantes conhecidos, estado e placar registrado.</summary>
/// <param name="Id">GUID interno da partida.</param>
/// <param name="EditionId">GUID interno da edição à qual a partida pertence.</param>
/// <param name="PhaseId">Fase cadastrada, quando conhecida.</param>
/// <param name="RoundId">Rodada cadastrada, quando conhecida.</param>
/// <param name="Home">Equipe mandante; null se o participante ainda não foi resolvido.</param>
/// <param name="Away">Equipe visitante; null se o participante ainda não foi resolvido.</param>
/// <param name="ScheduledAt">Instante agendado em ISO 8601 com offset; null se ainda desconhecido.</param>
/// <param name="Venue">Valor de local importado; pode ser um identificador da fonte, ainda sem resolução para nome.</param>
/// <param name="Title">Título original informado pela fonte.</param>
/// <param name="Status">Estado: Scheduled, Live, Finished, Cancelled ou Unknown.</param>
/// <param name="HomePenalties">Pênaltis convertidos pelo mandante; null quando não informado.</param>
/// <param name="AwayPenalties">Pênaltis convertidos pelo visitante; separados dos gols da partida.</param>
/// <param name="HomeScore">Gols do mandante; null indica placar desconhecido e não zero.</param>
/// <param name="AwayScore">Gols do visitante; null indica placar desconhecido e não zero.</param>
/// <param name="Revision">Revisão dos dados da partida, utilizada para detectar súmulas desatualizadas.</param>
public sealed record MatchResponse(Guid Id, Guid EditionId, Guid? PhaseId, Guid? RoundId,
    TeamResponse? Home, TeamResponse? Away, DateTimeOffset? ScheduledAt, string? Venue,
    string? Title, string Status, int? HomeScore, int? AwayScore, int Revision, int? HomePenalties = null, int? AwayPenalties = null);

/// <summary>Estatísticas registradas de um jogador em uma partida.</summary>
/// <param name="PlayerId">GUID da identidade canônica; null quando não houver vínculo ativo.</param>
/// <param name="Name">Nome oficial, ou original quando não houver identidade vinculada.</param>
/// <param name="TeamId">GUID da equipe relacionada aos eventos da súmula.</param>
/// <param name="Goals">Gols registrados; null indica desconhecido.</param>
/// <param name="YellowCards">Cartões amarelos registrados; null indica desconhecido.</param>
/// <param name="RedCards">Cartões vermelhos registrados; null indica desconhecido.</param>
/// <param name="Appearances">Presença confirmada: 1; ausência confirmada: 0; desconhecida: null.</param>
/// <param name="IsComplete">Indica se os dados da súmula estão completos e correspondem à revisão atual da partida.</param>
/// <param name="SourcePlayerId">GUID do cadastro do jogador na edição, usado para obter a foto mesmo sem identidade canônica.</param>
public sealed record MatchPlayerResponse(Guid? PlayerId, string Name, Guid TeamId, int? Goals,
    int? YellowCards, int? RedCards, int? Appearances, bool IsComplete, Guid? SourcePlayerId = null);

/// <summary>Resultados da equipe em partidas encerradas com placares válidos; sem classificação por pontos.</summary>
/// <param name="Team">Identificação da equipe.</param>
/// <param name="Played">Quantidade de partidas encerradas consideradas.</param>
/// <param name="Wins">Vitórias registradas.</param>
/// <param name="Draws">Empates registrados.</param>
/// <param name="Losses">Derrotas registradas.</param>
/// <param name="GoalsFor">Total de gols marcados.</param>
/// <param name="GoalsAgainst">Total de gols sofridos.</param>
public sealed record TeamStatisticsResponse(TeamResponse Team, int Played, int Wins, int Draws,
    int Losses, long GoalsFor, long GoalsAgainst);

/// <summary>Premiação editorial confirmada ou reconhecimento automático de artilharia em edição encerrada.</summary>
/// <param name="Id">GUID da concessão, ou identificador estável do reconhecimento automático.</param>
/// <param name="Code">Código cadastrado da categoria do prêmio.</param>
/// <param name="Name">Nome oficial da categoria.</param>
/// <param name="Scope">Abrangência da categoria: Championship, Round ou Match.</param>
/// <param name="MatchId">Partida relacionada, quando o prêmio tiver esse contexto.</param>
/// <param name="RoundId">Rodada relacionada, quando o prêmio tiver esse contexto.</param>
/// <param name="SourcePlayerId">GUID do cadastro da copa premiado, ou null para premiação de técnico.</param>
/// <param name="PlayerName">Nome oficial do jogador premiado, quando aplicável.</param>
/// <param name="CoachId">GUID do técnico premiado, ou null para premiação de jogador.</param>
/// <param name="CoachName">Nome oficial do técnico premiado, quando aplicável.</param>
/// <param name="Position">Posição relacionada à seleção/premiação, quando cadastrada.</param>
/// <param name="ConfirmedAt">Instante de confirmação editorial; null para reconhecimento automático, sem data inventada.</param>
/// <param name="Automatic">True quando a artilharia da edição encerrada concede o reconhecimento sem concessão editorial.</param>
public sealed record AwardResponse(Guid Id, string Code, string Name, string Scope, Guid? MatchId,
    Guid? RoundId, Guid? SourcePlayerId, string? PlayerName, Guid? CoachId, string? CoachName,
    string? Position, DateTimeOffset? ConfirmedAt, bool Automatic = false);
