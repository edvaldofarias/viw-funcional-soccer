using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viw.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeGroupTeamsToPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamEntry_CompetitionGroup_EditionId_GroupId",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.DropIndex(
                name: "IX_TeamEntry_EditionId_GroupId",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CompetitionGroup_EditionId_PhaseId_Id",
                schema: "viw",
                table: "CompetitionGroup",
                columns: new[] { "EditionId", "PhaseId", "Id" });

            migrationBuilder.CreateTable(
                name: "CompetitionGroupTeam",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionGroupTeam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitionGroupTeam_CompetitionGroup_EditionId_PhaseId_GroupId",
                        columns: x => new { x.EditionId, x.PhaseId, x.GroupId },
                        principalSchema: "viw",
                        principalTable: "CompetitionGroup",
                        principalColumns: new[] { "EditionId", "PhaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionGroupTeam_TeamEntry_EditionId_TeamEntryId",
                        columns: x => new { x.EditionId, x.TeamEntryId },
                        principalSchema: "viw",
                        principalTable: "TeamEntry",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionGroupTeam_EditionId_PhaseId_GroupId",
                schema: "viw",
                table: "CompetitionGroupTeam",
                columns: new[] { "EditionId", "PhaseId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionGroupTeam_EditionId_PhaseId_TeamEntryId",
                schema: "viw",
                table: "CompetitionGroupTeam",
                columns: new[] { "EditionId", "PhaseId", "TeamEntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionGroupTeam_EditionId_TeamEntryId",
                schema: "viw",
                table: "CompetitionGroupTeam",
                columns: new[] { "EditionId", "TeamEntryId" });

            migrationBuilder.Sql("""
                INSERT INTO [viw].[CompetitionGroupTeam]
                    ([Id], [EditionId], [PhaseId], [GroupId], [TeamEntryId], [CreatedAt])
                SELECT NEWID(), t.[EditionId], g.[PhaseId], g.[Id], t.[Id], SYSDATETIMEOFFSET()
                FROM [viw].[TeamEntry] t
                JOIN [viw].[CompetitionGroup] g ON g.[EditionId] = t.[EditionId] AND g.[Id] = t.[GroupId];
                """);
            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "viw",
                table: "TeamEntry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitionGroupTeam",
                schema: "viw");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CompetitionGroup_EditionId_PhaseId_Id",
                schema: "viw",
                table: "CompetitionGroup");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "viw",
                table: "TeamEntry",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamEntry_EditionId_GroupId",
                schema: "viw",
                table: "TeamEntry",
                columns: new[] { "EditionId", "GroupId" });

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
    }
}
