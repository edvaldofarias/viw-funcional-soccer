using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viw.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "viw",
                table: "TeamEntry",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompetitionGroup",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionGroup", x => x.Id);
                    table.UniqueConstraint("AK_CompetitionGroup_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.ForeignKey(
                        name: "FK_CompetitionGroup_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionGroup_CompetitionPhase_EditionId_PhaseId",
                        columns: x => new { x.EditionId, x.PhaseId },
                        principalSchema: "viw",
                        principalTable: "CompetitionPhase",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamEntry_EditionId_GroupId",
                schema: "viw",
                table: "TeamEntry",
                columns: new[] { "EditionId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionGroup_EditionId_PhaseId_SortOrder",
                schema: "viw",
                table: "CompetitionGroup",
                columns: new[] { "EditionId", "PhaseId", "SortOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamEntry_CompetitionGroup_EditionId_GroupId",
                schema: "viw",
                table: "TeamEntry",
                columns: new[] { "EditionId", "GroupId" },
                principalSchema: "viw",
                principalTable: "CompetitionGroup",
                principalColumns: new[] { "EditionId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamEntry_CompetitionGroup_EditionId_GroupId",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.DropTable(
                name: "CompetitionGroup",
                schema: "viw");

            migrationBuilder.DropIndex(
                name: "IX_TeamEntry_EditionId_GroupId",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "viw",
                table: "TeamEntry");
        }
    }
}
