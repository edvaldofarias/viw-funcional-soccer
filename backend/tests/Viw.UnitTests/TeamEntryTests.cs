using Shouldly;
using Viw.Domain.Competition;

namespace Viw.UnitTests;

public sealed class TeamEntryTests
{
    [Fact]
    public void Create_rejects_an_empty_edition_id()
    {
        Should.Throw<ArgumentException>(() => TeamEntry.Create(Guid.Empty, "fixture", "team-1", "Equipe"));
    }

    [Fact]
    public void Update_from_source_rejects_negative_order_without_changing_existing_data()
    {
        var team = TeamEntry.Create(Guid.NewGuid(), "fixture", "team-1", "Equipe");
        team.UpdateFromSource("Nome importado", null, """{"group":{}}""", 0);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            team.UpdateFromSource("Nome inválido", null, null, -1));

        team.Name.ShouldBe("Nome importado");
        team.SourceResultOrder.ShouldBe(0);
        team.AggregateStatisticsJson.ShouldBe("""{"group":{}}""");
    }

    [Fact]
    public void Source_image_asset_rejects_an_empty_id()
    {
        var team = TeamEntry.Create(Guid.NewGuid(), "fixture", "team-1", "Equipe");

        Should.Throw<ArgumentException>(() => team.SetSourceImageAsset(Guid.Empty));
    }
}
