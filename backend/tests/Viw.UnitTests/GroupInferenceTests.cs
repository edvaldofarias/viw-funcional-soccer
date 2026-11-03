using Viw.Infrastructure.Imports;

namespace Viw.UnitTests;

public sealed class GroupInferenceTests
{
    [Fact]
    public void Round_robin_single_component_produces_no_groups()
    {
        // Arrange: pontos corridos — todos jogam contra todos.
        var teams = Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToArray();
        var matches = new List<(Guid, Guid)>();
        for (var i = 0; i < teams.Length; i++)
            for (var j = i + 1; j < teams.Length; j++)
                matches.Add((teams[i], teams[j]));

        // Act
        var groups = CompetitionImportWriter.InferGroups(matches);

        // Assert
        groups.ShouldBeEmpty();
    }

    [Fact]
    public void Crossed_groups_are_separated_and_named_stably()
    {
        // Arrange: Grupo A x Grupo B — nenhum jogo dentro do mesmo grupo.
        var a1 = Guid.NewGuid(); var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid(); var b2 = Guid.NewGuid();
        var names = new Dictionary<Guid, string> { [a1] = "Vitória", [a2] = "Ceará", [b1] = "Sport", [b2] = "Náutico" };
        var matches = new List<(Guid, Guid)> { (a1, b1), (a1, b2), (a2, b1), (a2, b2) };

        // Act
        var groups = CompetitionImportWriter.InferGroups(matches, id => names[id]);

        // Assert
        groups.Count.ShouldBe(2);
        // Ordenação pelo menor nome do componente: Ceará/Vitória antes de Náutico/Sport.
        groups[0].ShouldBe([a1, a2], ignoreOrder: true);
        groups[1].ShouldBe([b1, b2], ignoreOrder: true);
    }

    [Fact]
    public void Internal_groups_without_cross_matches_are_separated()
    {
        // Arrange: dois grupos internos sem confrontos cruzados.
        var a1 = Guid.NewGuid(); var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid(); var b2 = Guid.NewGuid();
        var matches = new List<(Guid, Guid)> { (a1, a2), (b1, b2) };

        // Act
        var groups = CompetitionImportWriter.InferGroups(matches);

        // Assert
        groups.Count.ShouldBe(2);
        groups.SelectMany(x => x).ShouldBe([a1, a2, b1, b2], ignoreOrder: true);
    }

    [Fact]
    public void Single_bridge_match_merges_everything_into_one_group()
    {
        // Arrange: um único confronto cruzado conecta todos — não há grupos separados.
        var a1 = Guid.NewGuid(); var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid(); var b2 = Guid.NewGuid();
        var matches = new List<(Guid, Guid)> { (a1, a2), (b1, b2), (a2, b1) };

        // Act
        var groups = CompetitionImportWriter.InferGroups(matches);

        // Assert
        groups.ShouldBeEmpty();
    }
}
