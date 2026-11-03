using Viw.Infrastructure.Queries;

namespace Viw.UnitTests;

public sealed class PlayerNameSearchTests
{
    [Theory]
    [InlineData("Edvaldo Farias", "Edvaldo Farias", true)]
    [InlineData("Edvaldo", "Edvaldo Farias", true)]
    [InlineData("Farias", "Edvaldo Farias", true)]
    [InlineData("E. Santana", "Edvaldo Farias", true)]
    [InlineData("E Santana", "Edvaldo", true)]
    [InlineData("EDVALDO", "e. santana", true)]
    [InlineData("Santana", "E. Santana", true)]
    [InlineData("Bruno", "Edvaldo Farias", false)]
    [InlineData("Jose Teresa", "E.", false)]
    [InlineData("Edvaldo", "valdo", true)]
    [InlineData("Degas", "de", true)]
    [InlineData("Bruno", ".", false)]
    [InlineData("Bruno", "", true)]
    [InlineData("Bruno", null, true)]
    [InlineData("João da Silva", "joao", true)]
    [InlineData("João da Silva", "JOA\u0303O", true)]
    [InlineData("José Luís", "jose luis", true)]
    [InlineData("CAIPIRA", "caipra", true)]
    [InlineData("EDVALDO", "edvlado", true)]
    [InlineData("ADRIANO", "adraino", true)]
    [InlineData("ARMANDINHO", "armandino", true)]
    [InlineData("João", "jao", true)]
    [InlineData("Bruno", "XYZ", false)]
    [InlineData("Rui", "Lu", false)]
    public void Names_match_accents_typos_parts_and_initials_without_matching_arbitrary_inner_letters(
        string name, string? search, bool expected)
    {
        // Arrange
        var candidate = new PlayerNameSearch.Candidate(Guid.NewGuid(), name);
        // Act
        var result = PlayerNameSearch.Rank([candidate], search).Length > 0;
        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Nicknames_are_searched_and_null_names_are_safe()
    {
        // Arrange
        var nickname = new PlayerNameSearch.Candidate(Guid.NewGuid(), null, null, "Fárias");
        var missing = new PlayerNameSearch.Candidate(Guid.NewGuid(), null);
        // Act
        var result = PlayerNameSearch.Rank([nickname, missing], "Edvaldo Farias");
        // Assert
        result.Single().Id.ShouldBe(nickname.Id);
    }

    [Fact]
    public void Exact_names_precede_partial_initial_and_fuzzy_matches_and_aliases_do_not_duplicate_players()
    {
        var exact = new PlayerNameSearch.Candidate(Guid.NewGuid(), "João");
        var partial = new PlayerNameSearch.Candidate(Guid.NewGuid(), "João Silva");
        var initial = new PlayerNameSearch.Candidate(Guid.NewGuid(), "J. Santana");
        var fuzzy = new PlayerNameSearch.Candidate(Guid.NewGuid(), "Jão");
        var alias = exact with { OriginalName = "Jão" };

        var result = PlayerNameSearch.Rank([fuzzy, initial, partial, alias, exact], "joao");

        result.Select(x => x.Id).ShouldBe([exact.Id, partial.Id, initial.Id, fuzzy.Id]);
        result.Select(x => x.Score).ShouldBe(result.Select(x => x.Score).OrderDescending());
    }

    [Fact]
    public async Task Concurrent_searches_have_independent_reusable_distance_buffers()
    {
        var candidate = new PlayerNameSearch.Candidate(Guid.NewGuid(), "EDVALDO");
        var searches = Enumerable.Range(0, 40).Select(i => Task.Run(() =>
            PlayerNameSearch.Rank([candidate], i % 2 == 0 ? "edvlado" : "bruno")));

        var results = await Task.WhenAll(searches);

        for (var i = 0; i < results.Length; i++) results[i].Length.ShouldBe(i % 2 == 0 ? 1 : 0);
    }

    [Fact]
    public void Search_honors_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Should.Throw<OperationCanceledException>(() => PlayerNameSearch.Rank(
            [new(Guid.NewGuid(), "Edvaldo")], "edvlado", cancellation.Token));
    }
}
