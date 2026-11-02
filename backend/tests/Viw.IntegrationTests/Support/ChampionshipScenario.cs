using Viw.Domain.Competition;

namespace Viw.IntegrationTests.Support;

/// <summary>Dados sintéticos reutilizáveis; cada instância tem IDs próprios.</summary>
internal sealed class ChampionshipScenario
{
    public Competition Competition { get; } = new() { Name = "Teste", Slug = "teste" };
    public ChampionshipEdition Edition { get; }
    public ChampionshipEdition OtherEdition { get; }
    public TeamEntry Home { get; }
    public TeamEntry Away { get; }
    public TeamEntry ForeignTeam { get; }
    public SourcePlayer SourcePlayer { get; }
    public Player Player { get; } = new() { DisplayName = "Pessoa A", Slug = "pessoa-a" };
    public Player OtherPlayer { get; } = new() { DisplayName = "Pessoa B", Slug = "pessoa-b" };
    public AwardDefinition TopScorer { get; } = new()
    {
        Code = "top-scorer", Name = "Artilheiro", Scope = AwardScope.Championship,
        RecipientKind = RecipientKind.Player
    };

    public ChampionshipScenario()
    {
        Edition = CreateEdition("a", 2026, 2);
        OtherEdition = CreateEdition("b", 2025, 1);
        Home = CreateTeam(Edition.Id, "team-A");
        Away = CreateTeam(Edition.Id, "team-a");
        ForeignTeam = CreateTeam(OtherEdition.Id, "team-A");
        SourcePlayer = new SourcePlayer
        {
            EditionId = Edition.Id, Provider = "fixture", ExternalId = "player-a", OriginalName = "Nome original"
        };
    }

    public object[] SeedEntities =>
        [Competition, Edition, OtherEdition, Home, Away, ForeignTeam, SourcePlayer, Player, OtherPlayer, TopScorer];

    public TeamEntry DuplicateHomeTeam() => CreateTeam(Edition.Id, Home.ExternalId);

    public Match CreateMatch() => new()
    {
        EditionId = Edition.Id, Provider = "fixture", ExternalId = "match-a",
        HomeTeamId = Home.Id, AwayTeamId = Away.Id, Revision = 1
    };

    public PlayerIdentityLink CreateIdentityLink(Guid? playerId = null) => new()
    {
        EditionId = Edition.Id, SourcePlayerId = SourcePlayer.Id, PlayerId = playerId ?? Player.Id,
        LinkedAt = DateTimeOffset.UtcNow, LinkedBy = "test", Reason = "Conferido"
    };

    public AwardGrant CreateTopScorerAward(Guid? playerId = null) => new()
    {
        EditionId = Edition.Id, AwardDefinitionId = TopScorer.Id, SourcePlayerId = playerId ?? SourcePlayer.Id,
        Status = AwardStatus.Confirmed, ConfirmedAt = DateTimeOffset.UtcNow,
        DecidedBy = "test", Reason = "Premiação oficial"
    };

    private ChampionshipEdition CreateEdition(string suffix, int year, int semester) => new()
    {
        CompetitionId = Competition.Id, Name = "Teste " + suffix, Slug = "teste-" + suffix,
        Year = year, Semester = semester, TimeZoneId = "America/Sao_Paulo"
    };

    private static TeamEntry CreateTeam(Guid editionId, string externalId) => new()
    {
        EditionId = editionId, Provider = "fixture", ExternalId = externalId, Name = externalId
    };
}
