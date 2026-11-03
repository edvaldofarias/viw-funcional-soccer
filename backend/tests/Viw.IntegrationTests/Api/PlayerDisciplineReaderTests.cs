using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Viw.Domain.Competition;
using Viw.Infrastructure.Imports;
using Viw.Infrastructure.Persistence;
using Viw.Infrastructure.Statistics;

namespace Viw.IntegrationTests.Api;

public sealed class PlayerDisciplineReaderTests
{
    [Theory]
    [InlineData(true, """{"totals":{"14":5,"16":2}}""", 5, 2)]
    [InlineData(true, """{"totals":{}}""", 1, 0)]
    [InlineData(true, """{"totals":{"14":0,"16":0}}""", 0, 0)]
    [InlineData(false, """{"totals":{"14":99,"16":99}}""", 1, 0)]
    public async Task Reader_resolves_edition_cards_without_counting_live_matches(
        bool archived, string aggregate, long yellow, long red)
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ViwDbContext(new DbContextOptionsBuilder<ViwDbContext>().UseSqlite(connection).Options);
        // Apenas colunas consultadas; migrations e constraints continuam cobertas pelos testes SQL Server.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE ChampionshipEdition (Id TEXT PRIMARY KEY, Status INTEGER);
            CREATE TABLE SourcePlayer (Id TEXT PRIMARY KEY, EditionId TEXT, Provider TEXT, AggregateStatisticsJson TEXT);
            CREATE TABLE Match (Id TEXT PRIMARY KEY, Status INTEGER);
            CREATE TABLE PlayerMatchStatistics (SourcePlayerId TEXT, MatchId TEXT, YellowCards INTEGER, RedCards INTEGER);
            """);
        var editionId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var finishedId = Guid.NewGuid();
        var liveId = Guid.NewGuid();
        var status = archived ? EditionStatus.Archived : EditionStatus.Active;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO ChampionshipEdition VALUES ({editionId}, {(int)status})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO SourcePlayer VALUES ({sourceId}, {editionId}, {ChampionshipCatalog.Provider}, {aggregate})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Match VALUES ({finishedId}, {(int)MatchStatus.Finished}), ({liveId}, {(int)MatchStatus.Live})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO PlayerMatchStatistics VALUES ({sourceId}, {finishedId}, 1, 0), ({sourceId}, {liveId}, 8, 4)");
        // Act
        var result = await new PlayerDisciplineReader(db).ReadAsync(
            db.SourcePlayerRecords.Where(x => x.Id == sourceId), default);
        // Assert
        result.Single().ShouldBe(new SourcePlayerDiscipline(sourceId, editionId, yellow, red));
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }
}
