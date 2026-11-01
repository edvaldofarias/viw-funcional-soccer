using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viw.Domain.Competition;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Persistence;

public sealed class SqlServerSchemaTests
{
    [SqlServerFact]
    public async Task Migrations_are_applied_to_the_isolated_database()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();

        var expected = database.Context.Database.GetMigrations().ToArray();

        // Act
        var applied = await database.Context.Database.GetAppliedMigrationsAsync();

        // Assert
        expected.ShouldNotBeEmpty();
        applied.ShouldBe(expected);
    }

    [SqlServerFact]
    public async Task External_ids_are_case_sensitive_but_exact_duplicates_are_rejected()
    {
        // Arrange: o cenário contém team-A e team-a na mesma edição.
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var context = database.Context;
        context.Add(database.Data.DuplicateHomeTeam());

        // Act
        var exception = await Record.ExceptionAsync(() => context.SaveChangesAsync());

        // Assert
        var update = exception.ShouldBeOfType<DbUpdateException>();
        var sql = update.InnerException.ShouldBeOfType<SqlException>();
        new[] { 2601, 2627 }.ShouldContain(sql.Number);
    }

    [SqlServerFact]
    public async Task Match_cannot_use_a_team_from_another_edition()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var match = database.Data.CreateMatch();
        match.HomeTeamId = database.Data.ForeignTeam.Id;
        database.Context.Add(match);

        // Act
        var exception = await Record.ExceptionAsync(() => database.Context.SaveChangesAsync());

        // Assert
        var update = exception.ShouldBeOfType<DbUpdateException>();
        update.InnerException.ShouldBeOfType<SqlException>().Number.ShouldBe(547);
    }

    [SqlServerFact]
    public async Task Finished_match_requires_a_normalized_score()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var match = database.Data.CreateMatch();
        match.Status = MatchStatus.Finished;
        database.Context.Add(match);

        // Act
        var exception = await Record.ExceptionAsync(() => database.Context.SaveChangesAsync());

        // Assert
        var update = exception.ShouldBeOfType<DbUpdateException>();
        update.InnerException.ShouldBeOfType<SqlException>().Number.ShouldBe(547);
    }

    [SqlServerFact]
    public async Task Normalized_zero_score_is_persisted()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var score = MatchScore.FromRecordedResult(null, 3);
        var match = database.Data.CreateMatch();
        match.Status = MatchStatus.Finished;
        match.HomeScore = score.Home;
        match.AwayScore = score.Away;
        database.Context.Add(match);

        // Act
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.MatchRecords.SingleAsync();

        // Assert
        stored.HomeScore.ShouldBe(0);
        stored.AwayScore.ShouldBe(3);
    }

    [SqlServerFact]
    public async Task Source_player_cannot_have_two_active_identities()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        database.Context.Add(database.Data.CreateIdentityLink());
        await database.Context.SaveChangesAsync();
        database.Context.Add(database.Data.CreateIdentityLink(database.Data.OtherPlayer.Id));

        // Act
        var exception = await Record.ExceptionAsync(() => database.Context.SaveChangesAsync());

        // Assert
        var update = exception.ShouldBeOfType<DbUpdateException>();
        var sql = update.InnerException.ShouldBeOfType<SqlException>();
        new[] { 2601, 2627 }.ShouldContain(sql.Number);
    }

    [SqlServerFact]
    public async Task Identity_can_be_reassigned_without_erasing_history_or_original_name()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var original = database.Data.CreateIdentityLink();
        database.Context.Add(original);
        await database.Context.SaveChangesAsync();
        original.UnlinkedAt = DateTimeOffset.UtcNow;
        original.UnlinkedBy = "test";
        await database.Context.SaveChangesAsync();
        var corrected = database.Data.CreateIdentityLink(database.Data.OtherPlayer.Id);

        // Act
        database.Context.Add(corrected);
        await database.Context.SaveChangesAsync();
        var links = await database.Context.PlayerIdentityLinkRecords.ToListAsync();
        var source = await database.Context.SourcePlayerRecords.SingleAsync();

        // Assert
        links.Count.ShouldBe(2);
        var active = links.Where(link => link.UnlinkedAt is null).ShouldHaveSingleItem();
        active.PlayerId.ShouldBe(database.Data.OtherPlayer.Id);
        source.OriginalName.ShouldBe("Nome original");
    }

    [SqlServerFact]
    public async Task Top_scorer_tie_allows_two_winners_but_not_a_duplicate_award()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        database.Context.AddRange(database.Data.CreateTopScorerAward(),
            database.Data.CreateTopScorerAward(database.Data.OtherPlayer.Id));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        database.Context.Add(database.Data.CreateTopScorerAward());

        // Act
        var exception = await Record.ExceptionAsync(() => database.Context.SaveChangesAsync());
        var persistedAwards = await database.Context.AwardGrantRecords.CountAsync();

        // Assert
        var update = exception.ShouldBeOfType<DbUpdateException>();
        var sql = update.InnerException.ShouldBeOfType<SqlException>();
        new[] { 2601, 2627 }.ShouldContain(sql.Number);
        persistedAwards.ShouldBe(2);
    }
}
