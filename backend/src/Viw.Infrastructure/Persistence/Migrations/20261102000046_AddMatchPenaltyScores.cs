using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viw.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchPenaltyScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayPenalties",
                schema: "viw",
                table: "Match",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomePenalties",
                schema: "viw",
                table: "Match",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayPenalties",
                schema: "viw",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "HomePenalties",
                schema: "viw",
                table: "Match");
        }
    }
}
