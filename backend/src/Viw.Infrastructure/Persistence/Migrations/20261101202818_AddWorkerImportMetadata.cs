using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viw.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerImportMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AggregateStatisticsJson",
                schema: "viw",
                table: "TeamEntry",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceImageAssetId",
                schema: "viw",
                table: "TeamEntry",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AggregateStatisticsJson",
                schema: "viw",
                table: "SourcePlayer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceImageAssetId",
                schema: "viw",
                table: "SourcePlayer",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsComplete",
                schema: "viw",
                table: "PlayerMatchStatistics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                schema: "viw",
                table: "MatchSourceEvent",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "viw",
                table: "CompetitionRound",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceCoverAssetId",
                schema: "viw",
                table: "ChampionshipEdition",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCoverUrl",
                schema: "viw",
                table: "ChampionshipEdition",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceImageAssetId",
                schema: "viw",
                table: "ChampionshipEdition",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceImageUrl",
                schema: "viw",
                table: "ChampionshipEdition",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamEntry_SourceImageAssetId",
                schema: "viw",
                table: "TeamEntry",
                column: "SourceImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePlayer_SourceImageAssetId",
                schema: "viw",
                table: "SourcePlayer",
                column: "SourceImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionRound_EditionId_PhaseId_ExternalId",
                schema: "viw",
                table: "CompetitionRound",
                columns: new[] { "EditionId", "PhaseId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipEdition_SourceCoverAssetId",
                schema: "viw",
                table: "ChampionshipEdition",
                column: "SourceCoverAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipEdition_SourceImageAssetId",
                schema: "viw",
                table: "ChampionshipEdition",
                column: "SourceImageAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChampionshipEdition_MediaAsset_SourceCoverAssetId",
                schema: "viw",
                table: "ChampionshipEdition",
                column: "SourceCoverAssetId",
                principalSchema: "viw",
                principalTable: "MediaAsset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChampionshipEdition_MediaAsset_SourceImageAssetId",
                schema: "viw",
                table: "ChampionshipEdition",
                column: "SourceImageAssetId",
                principalSchema: "viw",
                principalTable: "MediaAsset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SourcePlayer_MediaAsset_SourceImageAssetId",
                schema: "viw",
                table: "SourcePlayer",
                column: "SourceImageAssetId",
                principalSchema: "viw",
                principalTable: "MediaAsset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamEntry_MediaAsset_SourceImageAssetId",
                schema: "viw",
                table: "TeamEntry",
                column: "SourceImageAssetId",
                principalSchema: "viw",
                principalTable: "MediaAsset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChampionshipEdition_MediaAsset_SourceCoverAssetId",
                schema: "viw",
                table: "ChampionshipEdition");

            migrationBuilder.DropForeignKey(
                name: "FK_ChampionshipEdition_MediaAsset_SourceImageAssetId",
                schema: "viw",
                table: "ChampionshipEdition");

            migrationBuilder.DropForeignKey(
                name: "FK_SourcePlayer_MediaAsset_SourceImageAssetId",
                schema: "viw",
                table: "SourcePlayer");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamEntry_MediaAsset_SourceImageAssetId",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.DropIndex(
                name: "IX_TeamEntry_SourceImageAssetId",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.DropIndex(
                name: "IX_SourcePlayer_SourceImageAssetId",
                schema: "viw",
                table: "SourcePlayer");

            migrationBuilder.DropIndex(
                name: "IX_CompetitionRound_EditionId_PhaseId_ExternalId",
                schema: "viw",
                table: "CompetitionRound");

            migrationBuilder.DropIndex(
                name: "IX_ChampionshipEdition_SourceCoverAssetId",
                schema: "viw",
                table: "ChampionshipEdition");

            migrationBuilder.DropIndex(
                name: "IX_ChampionshipEdition_SourceImageAssetId",
                schema: "viw",
                table: "ChampionshipEdition");

            migrationBuilder.DropColumn(
                name: "AggregateStatisticsJson",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.DropColumn(
                name: "SourceImageAssetId",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.DropColumn(
                name: "AggregateStatisticsJson",
                schema: "viw",
                table: "SourcePlayer");

            migrationBuilder.DropColumn(
                name: "SourceImageAssetId",
                schema: "viw",
                table: "SourcePlayer");

            migrationBuilder.DropColumn(
                name: "IsComplete",
                schema: "viw",
                table: "PlayerMatchStatistics");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                schema: "viw",
                table: "MatchSourceEvent");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "viw",
                table: "CompetitionRound");

            migrationBuilder.DropColumn(
                name: "SourceCoverAssetId",
                schema: "viw",
                table: "ChampionshipEdition");

            migrationBuilder.DropColumn(
                name: "SourceCoverUrl",
                schema: "viw",
                table: "ChampionshipEdition");

            migrationBuilder.DropColumn(
                name: "SourceImageAssetId",
                schema: "viw",
                table: "ChampionshipEdition");

            migrationBuilder.DropColumn(
                name: "SourceImageUrl",
                schema: "viw",
                table: "ChampionshipEdition");
        }
    }
}
