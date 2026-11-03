using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Admin;
using Viw.Infrastructure.Queries;
using Viw.IntegrationTests.Support;
using Xunit.Abstractions;

namespace Viw.IntegrationTests.Api;

public sealed class PlayerNameSearchSqlTests(ITestOutputHelper output)
{
    private static SourcePlayer Source(Guid edition, string name) => new()
    {
        EditionId = edition,
        Provider = "fixture",
        ExternalId = Guid.NewGuid().ToString("N"),
        OriginalName = name
    };

    [SqlServerFact]
    public async Task Compound_names_and_initials_match_separate_parts_while_team_and_edition_filters_and_team_labels_remain_correct()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var full = Source(data.Edition.Id, "Edvaldo Farias");
        var first = Source(data.Edition.Id, "Edvaldo");
        var last = Source(data.Edition.Id, "Farias");
        var initial = Source(data.Edition.Id, "E. Santana");
        var nickname = Source(data.Edition.Id, "Felipe"); nickname.Nickname = "Farias";
        var unrelated = Source(data.Edition.Id, "Bruno");
        var foreign = Source(data.OtherEdition.Id, "Edvaldo Farias");
        var now = DateTimeOffset.UtcNow;
        db.AddRange(full, first, last, initial, nickname, unrelated, foreign,
            new PlayerRegistration { EditionId = data.Edition.Id, SourcePlayerId = full.Id, TeamEntryId = data.Home.Id, ValidFrom = now },
            new PlayerRegistration { EditionId = data.Edition.Id, SourcePlayerId = full.Id, TeamEntryId = data.Home.Id, ValidFrom = now.AddDays(-2), ValidTo = now.AddDays(-1) },
            new PlayerRegistration { EditionId = data.Edition.Id, SourcePlayerId = full.Id, TeamEntryId = data.Away.Id, ValidFrom = now.AddDays(-1) },
            new PlayerRegistration { EditionId = data.Edition.Id, SourcePlayerId = initial.Id, TeamEntryId = data.Away.Id, ValidFrom = now });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var service = new AdminService(db);
        // Act
        var results = await service.SourcesAsync(data.Edition.Id, "edvaldo farias", 1, default);
        var home = await service.SourcesAsync(data.Edition.Id, "Edvaldo Farias", 1, default, data.Home.Id);
        // Assert
        results.Items.Select(x => x.Id).Order().ShouldBe(new[] { full.Id, first.Id, last.Id, initial.Id, nickname.Id }.Order());
        home.Items.Single().Id.ShouldBe(full.Id);
        home.Items.Single().Teams!.Select(x => x.Id).Order().ShouldBe(new[] { data.Home.Id, data.Away.Id }.Order());
        results.Items.Single(x => x.Id == first.Id).Teams.ShouldBeEmpty();
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Wiki_and_admin_targets_search_active_original_names_and_initials_without_duplicate_players_or_using_closed_links()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var original = await db.SourcePlayerRecords.SingleAsync(x => x.Id == data.SourcePlayer.Id);
        original.OriginalName = "E. Santana";
        var alias = Source(data.OtherEdition.Id, "Farias");
        var aliasLink = new PlayerIdentityLink
        {
            EditionId = alias.EditionId,
            SourcePlayerId = alias.Id,
            PlayerId = data.Player.Id,
            LinkedAt = DateTimeOffset.UtcNow,
            LinkedBy = "test",
            Reason = "Conferido"
        };
        db.AddRange(alias, Source(data.Edition.Id, "Edvaldo"), data.CreateIdentityLink(), aliasLink);
        await db.SaveChangesAsync();
        var service = new AdminService(db); var queries = new ViwQueries(db);
        // Act
        var wiki = await queries.PlayersAsync(new PlayerRequest { Search = "Edvaldo Farias" }, default);
        var targets = await service.PeopleAsync(false, "Edvaldo Farias", 1, default);
        // Assert
        wiki.Items.Single().Id.ShouldBe(data.Player.Id);
        targets.Items.Single().Id.ShouldBe(data.Player.Id);
        // Act
        aliasLink.UnlinkedAt = aliasLink.LinkedAt.AddMinutes(1); aliasLink.UnlinkedBy = "test";
        await db.SaveChangesAsync();
        var closedWiki = await queries.PlayersAsync(new PlayerRequest { Search = "Farias" }, default);
        var closedTargets = await service.PeopleAsync(false, "Farias", 1, default);
        // Assert
        closedWiki.Items.ShouldBeEmpty();
        closedTargets.Items.ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Broad_name_search_is_paginated_without_duplicates_even_when_multiple_terms_match()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var sources = Enumerable.Range(1, 35).Select(i => Source(data.Edition.Id, "Edvaldo Farias " + i)).ToArray();
        db.AddRange(sources); await db.SaveChangesAsync();
        var service = new AdminService(db);
        // Act
        var first = await service.SourcesAsync(data.Edition.Id, "Edvaldo Farias Edvaldo", 1, default);
        var second = await service.SourcesAsync(data.Edition.Id, "Edvaldo Farias Edvaldo", 2, default);
        // Assert
        first.TotalCount.ShouldBe(35); first.Items.Count.ShouldBe(30); second.Items.Count.ShouldBe(5);
        first.Items.Concat(second.Items).Select(x => x.Id).Order().ShouldBe(sources.Select(x => x.Id).Order());
    }

    [SqlServerFact]
    public async Task Accents_and_typos_are_ranked_before_paging_and_existing_source_filters_are_preserved()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var exact = Source(data.Edition.Id, "João");
        var fuzzy = Source(data.Edition.Id, "Jão");
        var nickname = Source(data.Edition.Id, "Bruno"); nickname.Nickname = "Caípira"; nickname.AssignedPosition = "Goleiro";
        var foreign = Source(data.OtherEdition.Id, "João");
        var linked = new PlayerIdentityLink
        {
            EditionId = data.Edition.Id, SourcePlayerId = exact.Id, PlayerId = data.Player.Id,
            LinkedAt = DateTimeOffset.UtcNow, LinkedBy = "test", Reason = "Conferido"
        };
        db.AddRange(exact, fuzzy, nickname, foreign, linked,
            new PlayerRegistration { EditionId = data.Edition.Id, SourcePlayerId = fuzzy.Id, TeamEntryId = data.Home.Id, ValidFrom = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var service = new AdminService(db);

        var names = await service.SourcesAsync(data.Edition.Id, "joao", 1, default);
        var team = await service.SourcesAsync(data.Edition.Id, "joao", 1, default, data.Home.Id);
        var unlinked = await service.SourcesAsync(data.Edition.Id, "joao", 1, default, unlinkedOnly: true);
        var keepers = await service.SourcesAsync(data.Edition.Id, "caipra", 1, default, goalkeepers: true);

        names.Items.Select(x => x.Id).ShouldBe([exact.Id, fuzzy.Id]);
        names.TotalCount.ShouldBe(2);
        team.Items.Single().Id.ShouldBe(fuzzy.Id);
        unlinked.Items.Single().Id.ShouldBe(fuzzy.Id);
        keepers.Items.Single().Id.ShouldBe(nickname.Id);
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Canonical_search_handles_official_accents_and_fuzzy_active_nicknames_and_has_no_stale_alias_cache()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var player = await db.PlayerRecords.SingleAsync(x => x.Id == data.Player.Id);
        var source = await db.SourcePlayerRecords.SingleAsync(x => x.Id == data.SourcePlayer.Id);
        player.DisplayName = "João Luís";
        source.Nickname = "Caípira";
        var link = data.CreateIdentityLink(); db.Add(link);
        await db.SaveChangesAsync();
        var admin = new AdminService(db); var wiki = new ViwQueries(db);

        foreach (var search in new[] { "joao luis", "jao", "caipra" })
        {
            (await admin.PeopleAsync(false, search, 1, default, data.Edition.Id)).Items.Single().Id.ShouldBe(player.Id);
            (await wiki.PlayersAsync(new PlayerRequest { Search = search, EditionId = data.Edition.Id }, default)).Items.Single().Id.ShouldBe(player.Id);
        }
        (await wiki.PlayersAsync(new PlayerRequest { Search = "joao", EditionId = data.OtherEdition.Id }, default)).Items.ShouldBeEmpty();
        (await admin.PeopleAsync(false, "joao", 1, default, data.OtherEdition.Id)).Items.ShouldBeEmpty();

        source.Nickname = "Armandinho"; await db.SaveChangesAsync();
        (await wiki.PlayersAsync(new PlayerRequest { Search = "caipra" }, default)).Items.ShouldBeEmpty();
        (await admin.PeopleAsync(false, "armandino", 1, default)).Items.Single().Id.ShouldBe(player.Id);
        link.UnlinkedAt = DateTimeOffset.UtcNow; link.UnlinkedBy = "test"; await db.SaveChangesAsync();
        (await admin.PeopleAsync(false, "armandino", 1, default)).Items.ShouldBeEmpty();
        (await wiki.PlayersAsync(new PlayerRequest { Search = "joao" }, default)).Items.ShouldBeEmpty();
        (await admin.PeopleAsync(false, "joao", 1, default)).Items.Single().Id.ShouldBe(player.Id);
    }

    [SqlServerFact]
    public async Task Canonical_fuzzy_results_keep_complete_counts_and_stable_non_overlapping_pages()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var players = Enumerable.Range(0, 35).Select(i => new Player { DisplayName = "João Silva " + i, Slug = "joao-" + i }).ToArray();
        foreach (var player in players)
        {
            var source = Source(data.Edition.Id, "Origem");
            db.AddRange(player, source, new PlayerIdentityLink
            {
                EditionId = data.Edition.Id, SourcePlayerId = source.Id, PlayerId = player.Id,
                LinkedAt = DateTimeOffset.UtcNow, LinkedBy = "test", Reason = "Conferido"
            });
        }
        await db.SaveChangesAsync();
        var admin = new AdminService(db); var wiki = new ViwQueries(db);

        var first = await wiki.PlayersAsync(new PlayerRequest { Search = "jao", PageSize = 20 }, default);
        var second = await wiki.PlayersAsync(new PlayerRequest { Search = "jao", PageSize = 20, Page = 2 }, default);
        var adminFirst = await admin.PeopleAsync(false, "jao", 1, default);
        var adminSecond = await admin.PeopleAsync(false, "jao", 2, default);

        first.TotalCount.ShouldBe(35); second.TotalCount.ShouldBe(35);
        first.Items.Count.ShouldBe(20); second.Items.Count.ShouldBe(15);
        adminFirst.TotalCount.ShouldBe(35); adminSecond.Items.Count.ShouldBe(5);
        first.Items.Concat(second.Items).Select(x => x.Id).ShouldBe(adminFirst.Items.Concat(adminSecond.Items).Select(x => x.Id));
        first.Items.Concat(second.Items).Select(x => x.Id).Distinct().Count().ShouldBe(35);
        (await wiki.PlayersAsync(new PlayerRequest { Search = "jao", Page = 3 }, default)).Items.ShouldBeEmpty();
        (await admin.PeopleAsync(false, "jao", 3, default)).TotalCount.ShouldBe(35);
    }

    [SqlServerFact]
    public async Task Measure_scoped_fuzzy_search_with_three_thousand_sources()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var match = Source(data.Edition.Id, "Edvaldo");
        db.Add(match);
        db.AddRange(Enumerable.Range(0, 3000).Select(i => Source(data.Edition.Id, "ZZZZZZ NNNNNN " + i)));
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var service = new AdminService(db);
        var candidateCount = await db.SourcePlayerRecords.CountAsync(x => x.EditionId == data.Edition.Id);
        await service.SourcesAsync(data.Edition.Id, "edvlado", 1, default);
        var elapsed = new List<double>();
        for (var i = 0; i < 20; i++)
        {
            var watch = Stopwatch.StartNew();
            var result = await service.SourcesAsync(data.Edition.Id, "edvlado", 1, default);
            elapsed.Add(watch.Elapsed.TotalMilliseconds);
            result.Items.Single().Id.ShouldBe(match.Id);
            result.TotalCount.ShouldBe(1);
        }
        elapsed.Sort();
        output.WriteLine($"Busca SQL + fuzzy + detalhes da página, {candidateCount} nomes e 20 consultas: mediana {elapsed[10]:F1} ms; p95 {elapsed[18]:F1} ms.");
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }
}
