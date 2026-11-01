using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Viw.Domain.Competition;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Persistence;

public sealed class ModelContractTests
{
    [Fact]
    public void Relationships_are_restrictive_and_scoped_to_edition()
    {
        // Arrange
        using var context = ModelContextFactory.Create();

        // Act
        var foreignKeys = context.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()).ToArray();
        var match = context.Model.FindEntityType(typeof(Match))!;
        var homeKeys = match.GetForeignKeys().Where(key => key.Properties.Any(p => p.Name == "HomeTeamId")).ToArray();

        // Assert
        foreignKeys.ShouldAllBe(key => key.DeleteBehavior == DeleteBehavior.Restrict);
        var home = homeKeys.ShouldHaveSingleItem();
        home.Properties.Select(property => property.Name).ShouldBe(new[] { "EditionId", "HomeTeamId" });
    }

    [Fact]
    public void Migration_chain_generates_sql_server_constraints_without_connecting()
    {
        // Arrange
        using var context = ModelContextFactory.Create();
        var migrator = context.GetService<IMigrator>();

        // Act
        var sql = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

        // Assert
        sql.ShouldContain("CREATE TABLE [viw].[Match]");
        sql.ShouldContain("CK_Match_Finished");
        sql.ShouldContain("Latin1_General_100_BIN2");
        sql.ShouldContain("[UnlinkedAt] IS NULL");
        sql.ShouldContain("CK_AwardGrant_Recipient");
    }

    [Fact]
    public void Model_matches_the_latest_migration()
    {
        // Arrange
        using var context = ModelContextFactory.Create();

        // Act
        var hasChanges = context.Database.HasPendingModelChanges();

        // Assert
        hasChanges.ShouldBe(false);
    }
    [Fact]
    public void Short_text_uses_unicode_with_default_length_of_100()
    {
        // Arrange
        using var context = ModelContextFactory.Create();

        // Act
        var position = context.Model.FindEntityType(typeof(SourcePlayer))!.FindProperty(nameof(SourcePlayer.Position))!;
        var failureCode = context.Model.FindEntityType(typeof(ImportRun))!.FindProperty(nameof(ImportRun.FailureCode))!;

        // Assert
        position.IsUnicode().ShouldBe(true);
        position.GetMaxLength().ShouldBe(100);
        position.GetColumnType().ShouldBe("nvarchar(100)");
        failureCode.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Specific_text_limits_override_the_default_without_limiting_json()
    {
        // Arrange
        using var context = ModelContextFactory.Create();
        var source = context.Model.FindEntityType(typeof(SourcePlayer))!;
        var snapshot = context.Model.FindEntityType(typeof(SourceSnapshot))!;

        // Act
        var name = source.FindProperty(nameof(SourcePlayer.OriginalName))!;
        var url = source.FindProperty(nameof(SourcePlayer.SourceImageUrl))!;
        var externalId = source.FindProperty(nameof(SourcePlayer.ExternalId))!;
        var rawTeam = context.Model.FindEntityType(typeof(Match))!.FindProperty(nameof(Match.RawHomeReference))!;
        var json = snapshot.FindProperty(nameof(SourceSnapshot.PayloadJson))!;
        var hash = snapshot.FindProperty(nameof(SourceSnapshot.ContentHash))!;

        // Assert
        name.GetMaxLength().ShouldBe(200);
        url.GetMaxLength().ShouldBe(2048);
        externalId.GetMaxLength().ShouldBe(128);
        rawTeam.GetMaxLength().ShouldBe(200);
        json.GetColumnType().ShouldBe("nvarchar(max)");
        json.GetMaxLength().ShouldBeNull();
        hash.IsUnicode().ShouldBe(false);
        hash.GetColumnType().ShouldBe("varchar(64)");
    }

    [Fact]
    public void Numeric_and_temporal_conventions_preserve_precision_and_offset()
    {
        // Arrange
        using var context = ModelContextFactory.Create();
        var sourceEvent = context.Model.FindEntityType(typeof(MatchSourceEvent))!;

        // Act
        var value = sourceEvent.FindProperty(nameof(MatchSourceEvent.Value))!;
        var createdAt = sourceEvent.FindProperty(nameof(MatchSourceEvent.CreatedAt))!;

        // Assert
        value.GetPrecision().ShouldBe(18);
        value.GetScale().ShouldBe(4);
        createdAt.GetColumnType().ShouldBe("datetimeoffset");
    }

}
