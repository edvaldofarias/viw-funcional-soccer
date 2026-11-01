using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viw.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateCompetitionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "viw");

            migrationBuilder.CreateTable(
                name: "AuditEntry",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntry", x => x.Id);
                    table.CheckConstraint("CK_AuditEntry_Json", "([BeforeJson] IS NULL OR ISJSON([BeforeJson]) = 1) AND ([AfterJson] IS NULL OR ISJSON([AfterJson]) = 1)");
                });

            migrationBuilder.CreateTable(
                name: "AwardDefinition",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    RecipientKind = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardDefinition", x => x.Id);
                    table.CheckConstraint("CK_AwardDefinition_Recipient", "[RecipientKind] IN (0,1)");
                    table.CheckConstraint("CK_AwardDefinition_Scope", "[Scope] IN (0,1,2)");
                });

            migrationBuilder.CreateTable(
                name: "Competition",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAsset",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ContentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAsset", x => x.Id);
                    table.CheckConstraint("CK_MediaAsset_Size", "[SizeBytes] > 0");
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastErrorCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.Id);
                    table.CheckConstraint("CK_OutboxMessage_Attempts", "[Attempts] >= 0");
                    table.CheckConstraint("CK_OutboxMessage_Payload", "ISJSON([PayloadJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipEdition",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipEdition", x => x.Id);
                    table.CheckConstraint("CK_ChampionshipEdition_Period", "[Year] BETWEEN 1900 AND 2200 AND [Semester] IN (1,2)");
                    table.CheckConstraint("CK_ChampionshipEdition_Status", "[Status] IN (0,1,2)");
                    table.ForeignKey(
                        name: "FK_ChampionshipEdition_Competition_CompetitionId",
                        column: x => x.CompetitionId,
                        principalSchema: "viw",
                        principalTable: "Competition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Coach",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OfficialImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coach", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coach_MediaAsset_OfficialImageId",
                        column: x => x.OfficialImageId,
                        principalSchema: "viw",
                        principalTable: "MediaAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OfficialImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Player_MediaAsset_OfficialImageId",
                        column: x => x.OfficialImageId,
                        principalSchema: "viw",
                        principalTable: "MediaAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionPhase",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionPhase", x => x.Id);
                    table.UniqueConstraint("AK_CompetitionPhase_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.ForeignKey(
                        name: "FK_CompetitionPhase_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EditionSource",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditionSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EditionSource_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportLease",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLease", x => x.Id);
                    table.CheckConstraint("CK_ImportLease_Generation", "[Generation] > 0");
                    table.ForeignKey(
                        name: "FK_ImportLease_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportRun",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecordsRead = table.Column<int>(type: "int", nullable: false),
                    RecordsChanged = table.Column<int>(type: "int", nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRun", x => x.Id);
                    table.UniqueConstraint("AK_ImportRun_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.CheckConstraint("CK_ImportRun_Counts", "[RecordsRead] >= 0 AND [RecordsChanged] >= 0");
                    table.CheckConstraint("CK_ImportRun_Status", "[Status] IN (0,1,2,3,4)");
                    table.CheckConstraint("CK_ImportRun_Times", "([CompletedAt] IS NULL OR ([StartedAt] IS NOT NULL AND [CompletedAt] >= [StartedAt])) AND ([Status] <> 1 OR ([StartedAt] IS NOT NULL AND [CompletedAt] IS NULL)) AND ([Status] NOT IN (2,3,4) OR [CompletedAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ImportRun_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet", x => x.Id);
                    table.CheckConstraint("CK_RuleSet_Json", "ISJSON([DefinitionJson]) = 1");
                    table.CheckConstraint("CK_RuleSet_Version", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_RuleSet_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SourcePlayer",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    OriginalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShirtNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcePlayer", x => x.Id);
                    table.UniqueConstraint("AK_SourcePlayer_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.ForeignKey(
                        name: "FK_SourcePlayer_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamEntry",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    OfficialImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamEntry", x => x.Id);
                    table.UniqueConstraint("AK_TeamEntry_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.ForeignKey(
                        name: "FK_TeamEntry_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamEntry_MediaAsset_OfficialImageId",
                        column: x => x.OfficialImageId,
                        principalSchema: "viw",
                        principalTable: "MediaAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionRound",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionRound", x => x.Id);
                    table.UniqueConstraint("AK_CompetitionRound_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.UniqueConstraint("AK_CompetitionRound_EditionId_PhaseId_Id", x => new { x.EditionId, x.PhaseId, x.Id });
                    table.CheckConstraint("CK_CompetitionRound_Number", "[Number] > 0");
                    table.ForeignKey(
                        name: "FK_CompetitionRound_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionRound_CompetitionPhase_EditionId_PhaseId",
                        columns: x => new { x.EditionId, x.PhaseId },
                        principalSchema: "viw",
                        principalTable: "CompetitionPhase",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SourceSnapshot",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourcePath = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ContentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    MapperVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FetchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrivateStorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceSnapshot", x => x.Id);
                    table.UniqueConstraint("AK_SourceSnapshot_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.CheckConstraint("CK_SourceSnapshot_Payload", "([PayloadJson] IS NOT NULL AND ISJSON([PayloadJson]) = 1 AND [PrivateStorageKey] IS NULL) OR ([PayloadJson] IS NULL AND [PrivateStorageKey] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SourceSnapshot_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceSnapshot_ImportRun_EditionId_ImportRunId",
                        columns: x => new { x.EditionId, x.ImportRunId },
                        principalSchema: "viw",
                        principalTable: "ImportRun",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerIdentityLink",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LinkedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnlinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UnlinkedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerIdentityLink", x => x.Id);
                    table.CheckConstraint("CK_PlayerIdentityLink_Interval", "([UnlinkedAt] IS NULL AND [UnlinkedBy] IS NULL) OR ([UnlinkedAt] >= [LinkedAt] AND [UnlinkedBy] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PlayerIdentityLink_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "viw",
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerIdentityLink_SourcePlayer_SourcePlayerId",
                        column: x => x.SourcePlayerId,
                        principalSchema: "viw",
                        principalTable: "SourcePlayer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerRegistration",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRegistration", x => x.Id);
                    table.UniqueConstraint("AK_PlayerRegistration_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.CheckConstraint("CK_PlayerRegistration_Interval", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.ForeignKey(
                        name: "FK_PlayerRegistration_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerRegistration_SourcePlayer_EditionId_SourcePlayerId",
                        columns: x => new { x.EditionId, x.SourcePlayerId },
                        principalSchema: "viw",
                        principalTable: "SourcePlayer",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerRegistration_TeamEntry_EditionId_TeamEntryId",
                        columns: x => new { x.EditionId, x.TeamEntryId },
                        principalSchema: "viw",
                        principalTable: "TeamEntry",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachAssignment",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoachId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AssignedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachAssignment", x => x.Id);
                    table.CheckConstraint("CK_CoachAssignment_Interval", "[EndsAt] IS NULL OR [EndsAt] >= [StartsAt]");
                    table.ForeignKey(
                        name: "FK_CoachAssignment_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachAssignment_Coach_CoachId",
                        column: x => x.CoachId,
                        principalSchema: "viw",
                        principalTable: "Coach",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachAssignment_CompetitionRound_EditionId_RoundId",
                        columns: x => new { x.EditionId, x.RoundId },
                        principalSchema: "viw",
                        principalTable: "CompetitionRound",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachAssignment_TeamEntry_EditionId_TeamEntryId",
                        columns: x => new { x.EditionId, x.TeamEntryId },
                        principalSchema: "viw",
                        principalTable: "TeamEntry",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Match",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    PhaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HomeTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AwayTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RawHomeReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RawAwayReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SourceTimeZoneOffsetMs = table.Column<long>(type: "bigint", nullable: true),
                    Venue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OriginalTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceStatus = table.Column<int>(type: "int", nullable: true),
                    SourceFinishedAtMs = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HomeScore = table.Column<int>(type: "int", nullable: true),
                    AwayScore = table.Column<int>(type: "int", nullable: true),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Match", x => x.Id);
                    table.UniqueConstraint("AK_Match_EditionId_Id", x => new { x.EditionId, x.Id });
                    table.CheckConstraint("CK_Match_Finished", "[Status] <> 2 OR ([HomeScore] IS NOT NULL AND [AwayScore] IS NOT NULL AND [HomeTeamId] IS NOT NULL AND [AwayTeamId] IS NOT NULL)");
                    table.CheckConstraint("CK_Match_Revision", "[Revision] > 0");
                    table.CheckConstraint("CK_Match_Round", "[RoundId] IS NULL OR [PhaseId] IS NOT NULL");
                    table.CheckConstraint("CK_Match_Score", "([HomeScore] IS NULL OR [HomeScore] >= 0) AND ([AwayScore] IS NULL OR [AwayScore] >= 0)");
                    table.CheckConstraint("CK_Match_Status", "[Status] IN (0,1,2,3,4)");
                    table.CheckConstraint("CK_Match_Teams", "[HomeTeamId] IS NULL OR [AwayTeamId] IS NULL OR [HomeTeamId] <> [AwayTeamId]");
                    table.ForeignKey(
                        name: "FK_Match_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_CompetitionPhase_EditionId_PhaseId",
                        columns: x => new { x.EditionId, x.PhaseId },
                        principalSchema: "viw",
                        principalTable: "CompetitionPhase",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_CompetitionRound_EditionId_PhaseId_RoundId",
                        columns: x => new { x.EditionId, x.PhaseId, x.RoundId },
                        principalSchema: "viw",
                        principalTable: "CompetitionRound",
                        principalColumns: new[] { "EditionId", "PhaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_TeamEntry_EditionId_AwayTeamId",
                        columns: x => new { x.EditionId, x.AwayTeamId },
                        principalSchema: "viw",
                        principalTable: "TeamEntry",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_TeamEntry_EditionId_HomeTeamId",
                        columns: x => new { x.EditionId, x.HomeTeamId },
                        principalSchema: "viw",
                        principalTable: "TeamEntry",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportCheckpoint",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourcePath = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Cursor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportCheckpoint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportCheckpoint_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportCheckpoint_SourceSnapshot_EditionId_LastSnapshotId",
                        columns: x => new { x.EditionId, x.LastSnapshotId },
                        principalSchema: "viw",
                        principalTable: "SourceSnapshot",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AwardGrant",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AwardDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoachId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardGrant", x => x.Id);
                    table.CheckConstraint("CK_AwardGrant_Confirmed", "[Status] <> 1 OR [ConfirmedAt] IS NOT NULL");
                    table.CheckConstraint("CK_AwardGrant_Context", "[MatchId] IS NULL OR [RoundId] IS NULL");
                    table.CheckConstraint("CK_AwardGrant_Recipient", "([PlayerId] IS NOT NULL AND [CoachId] IS NULL) OR ([CoachId] IS NOT NULL AND [PlayerId] IS NULL)");
                    table.CheckConstraint("CK_AwardGrant_Status", "[Status] IN (0,1,2)");
                    table.ForeignKey(
                        name: "FK_AwardGrant_AwardDefinition_AwardDefinitionId",
                        column: x => x.AwardDefinitionId,
                        principalSchema: "viw",
                        principalTable: "AwardDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardGrant_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardGrant_Coach_CoachId",
                        column: x => x.CoachId,
                        principalSchema: "viw",
                        principalTable: "Coach",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardGrant_CompetitionRound_EditionId_RoundId",
                        columns: x => new { x.EditionId, x.RoundId },
                        principalSchema: "viw",
                        principalTable: "CompetitionRound",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardGrant_Match_EditionId_MatchId",
                        columns: x => new { x.EditionId, x.MatchId },
                        principalSchema: "viw",
                        principalTable: "Match",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardGrant_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "viw",
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchSourceEvent",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ActionCode = table.Column<int>(type: "int", nullable: false),
                    SourcePlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeamEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSourceEvent", x => x.Id);
                    table.CheckConstraint("CK_MatchSourceEvent_Json", "ISJSON([RawJson]) = 1");
                    table.CheckConstraint("CK_MatchSourceEvent_Revision", "[Revision] > 0");
                    table.ForeignKey(
                        name: "FK_MatchSourceEvent_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSourceEvent_Match_EditionId_MatchId",
                        columns: x => new { x.EditionId, x.MatchId },
                        principalSchema: "viw",
                        principalTable: "Match",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSourceEvent_SourcePlayer_EditionId_SourcePlayerId",
                        columns: x => new { x.EditionId, x.SourcePlayerId },
                        principalSchema: "viw",
                        principalTable: "SourcePlayer",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSourceEvent_TeamEntry_EditionId_TeamEntryId",
                        columns: x => new { x.EditionId, x.TeamEntryId },
                        principalSchema: "viw",
                        principalTable: "TeamEntry",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMatchStatistics",
                schema: "viw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Goals = table.Column<int>(type: "int", nullable: true),
                    YellowCards = table.Column<int>(type: "int", nullable: true),
                    RedCards = table.Column<int>(type: "int", nullable: true),
                    Appearances = table.Column<int>(type: "int", nullable: true),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMatchStatistics", x => x.Id);
                    table.CheckConstraint("CK_PlayerMatchStatistics_Revision", "[Revision] > 0");
                    table.CheckConstraint("CK_PlayerMatchStatistics_Stats", "([Goals] IS NULL OR [Goals] >= 0) AND ([YellowCards] IS NULL OR [YellowCards] >= 0) AND ([RedCards] IS NULL OR [RedCards] >= 0) AND ([Appearances] IS NULL OR [Appearances] IN (0,1))");
                    table.ForeignKey(
                        name: "FK_PlayerMatchStatistics_ChampionshipEdition_EditionId",
                        column: x => x.EditionId,
                        principalSchema: "viw",
                        principalTable: "ChampionshipEdition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMatchStatistics_Match_EditionId_MatchId",
                        columns: x => new { x.EditionId, x.MatchId },
                        principalSchema: "viw",
                        principalTable: "Match",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMatchStatistics_SourcePlayer_EditionId_SourcePlayerId",
                        columns: x => new { x.EditionId, x.SourcePlayerId },
                        principalSchema: "viw",
                        principalTable: "SourcePlayer",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMatchStatistics_TeamEntry_EditionId_TeamEntryId",
                        columns: x => new { x.EditionId, x.TeamEntryId },
                        principalSchema: "viw",
                        principalTable: "TeamEntry",
                        principalColumns: new[] { "EditionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_EntityType_EntityId_OccurredAt",
                schema: "viw",
                table: "AuditEntry",
                columns: new[] { "EntityType", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AwardDefinition_Code",
                schema: "viw",
                table: "AwardDefinition",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AwardGrant_AwardDefinitionId",
                schema: "viw",
                table: "AwardGrant",
                column: "AwardDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardGrant_CoachId",
                schema: "viw",
                table: "AwardGrant",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardGrant_EditionId_AwardDefinitionId_MatchId_RoundId_PlayerId_CoachId_Position",
                schema: "viw",
                table: "AwardGrant",
                columns: new[] { "EditionId", "AwardDefinitionId", "MatchId", "RoundId", "PlayerId", "CoachId", "Position" },
                unique: true,
                filter: "[Status] <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_AwardGrant_EditionId_MatchId",
                schema: "viw",
                table: "AwardGrant",
                columns: new[] { "EditionId", "MatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_AwardGrant_EditionId_RoundId",
                schema: "viw",
                table: "AwardGrant",
                columns: new[] { "EditionId", "RoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_AwardGrant_PlayerId",
                schema: "viw",
                table: "AwardGrant",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipEdition_CompetitionId",
                schema: "viw",
                table: "ChampionshipEdition",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipEdition_Slug",
                schema: "viw",
                table: "ChampionshipEdition",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coach_OfficialImageId",
                schema: "viw",
                table: "Coach",
                column: "OfficialImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Coach_Slug",
                schema: "viw",
                table: "Coach",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachAssignment_CoachId",
                schema: "viw",
                table: "CoachAssignment",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachAssignment_EditionId_RoundId",
                schema: "viw",
                table: "CoachAssignment",
                columns: new[] { "EditionId", "RoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachAssignment_EditionId_TeamEntryId",
                schema: "viw",
                table: "CoachAssignment",
                columns: new[] { "EditionId", "TeamEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachAssignment_TeamEntryId_CoachId_StartsAt_RoundId",
                schema: "viw",
                table: "CoachAssignment",
                columns: new[] { "TeamEntryId", "CoachId", "StartsAt", "RoundId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Competition_Slug",
                schema: "viw",
                table: "Competition",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionPhase_EditionId_ExternalId",
                schema: "viw",
                table: "CompetitionPhase",
                columns: new[] { "EditionId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionRound_EditionId_PhaseId_Number",
                schema: "viw",
                table: "CompetitionRound",
                columns: new[] { "EditionId", "PhaseId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EditionSource_EditionId_Provider",
                schema: "viw",
                table: "EditionSource",
                columns: new[] { "EditionId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EditionSource_Provider_ExternalId",
                schema: "viw",
                table: "EditionSource",
                columns: new[] { "Provider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportCheckpoint_EditionId_LastSnapshotId",
                schema: "viw",
                table: "ImportCheckpoint",
                columns: new[] { "EditionId", "LastSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportCheckpoint_EditionId_ResourcePath",
                schema: "viw",
                table: "ImportCheckpoint",
                columns: new[] { "EditionId", "ResourcePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportLease_EditionId",
                schema: "viw",
                table: "ImportLease",
                column: "EditionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportRun_EditionId_CreatedAt",
                schema: "viw",
                table: "ImportRun",
                columns: new[] { "EditionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Match_EditionId_AwayTeamId",
                schema: "viw",
                table: "Match",
                columns: new[] { "EditionId", "AwayTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_Match_EditionId_HomeTeamId",
                schema: "viw",
                table: "Match",
                columns: new[] { "EditionId", "HomeTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_Match_EditionId_PhaseId_RoundId",
                schema: "viw",
                table: "Match",
                columns: new[] { "EditionId", "PhaseId", "RoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_Match_EditionId_Provider_ExternalId",
                schema: "viw",
                table: "Match",
                columns: new[] { "EditionId", "Provider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchSourceEvent_EditionId_MatchId",
                schema: "viw",
                table: "MatchSourceEvent",
                columns: new[] { "EditionId", "MatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchSourceEvent_EditionId_SourcePlayerId",
                schema: "viw",
                table: "MatchSourceEvent",
                columns: new[] { "EditionId", "SourcePlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchSourceEvent_EditionId_TeamEntryId",
                schema: "viw",
                table: "MatchSourceEvent",
                columns: new[] { "EditionId", "TeamEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchSourceEvent_MatchId_ExternalId",
                schema: "viw",
                table: "MatchSourceEvent",
                columns: new[] { "MatchId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_ContentHash",
                schema: "viw",
                table: "MediaAsset",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_StorageKey",
                schema: "viw",
                table: "MediaAsset",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ProcessedAt_OccurredAt",
                schema: "viw",
                table: "OutboxMessage",
                columns: new[] { "ProcessedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Player_OfficialImageId",
                schema: "viw",
                table: "Player",
                column: "OfficialImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_Slug",
                schema: "viw",
                table: "Player",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerIdentityLink_PlayerId",
                schema: "viw",
                table: "PlayerIdentityLink",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerIdentityLink_SourcePlayerId",
                schema: "viw",
                table: "PlayerIdentityLink",
                column: "SourcePlayerId",
                unique: true,
                filter: "[UnlinkedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMatchStatistics_EditionId_MatchId",
                schema: "viw",
                table: "PlayerMatchStatistics",
                columns: new[] { "EditionId", "MatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMatchStatistics_EditionId_SourcePlayerId",
                schema: "viw",
                table: "PlayerMatchStatistics",
                columns: new[] { "EditionId", "SourcePlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMatchStatistics_EditionId_TeamEntryId",
                schema: "viw",
                table: "PlayerMatchStatistics",
                columns: new[] { "EditionId", "TeamEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMatchStatistics_MatchId_SourcePlayerId",
                schema: "viw",
                table: "PlayerMatchStatistics",
                columns: new[] { "MatchId", "SourcePlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRegistration_EditionId_SourcePlayerId",
                schema: "viw",
                table: "PlayerRegistration",
                columns: new[] { "EditionId", "SourcePlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRegistration_EditionId_TeamEntryId",
                schema: "viw",
                table: "PlayerRegistration",
                columns: new[] { "EditionId", "TeamEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRegistration_SourcePlayerId_TeamEntryId_ValidFrom",
                schema: "viw",
                table: "PlayerRegistration",
                columns: new[] { "SourcePlayerId", "TeamEntryId", "ValidFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_EditionId_Version",
                schema: "viw",
                table: "RuleSet",
                columns: new[] { "EditionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourcePlayer_EditionId_Provider_ExternalId",
                schema: "viw",
                table: "SourcePlayer",
                columns: new[] { "EditionId", "Provider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceSnapshot_EditionId_ImportRunId",
                schema: "viw",
                table: "SourceSnapshot",
                columns: new[] { "EditionId", "ImportRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceSnapshot_EditionId_ResourcePath_ContentHash",
                schema: "viw",
                table: "SourceSnapshot",
                columns: new[] { "EditionId", "ResourcePath", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamEntry_EditionId_Provider_ExternalId",
                schema: "viw",
                table: "TeamEntry",
                columns: new[] { "EditionId", "Provider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamEntry_OfficialImageId",
                schema: "viw",
                table: "TeamEntry",
                column: "OfficialImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntry",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "AwardGrant",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "CoachAssignment",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "EditionSource",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "ImportCheckpoint",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "ImportLease",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "MatchSourceEvent",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "PlayerIdentityLink",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "PlayerMatchStatistics",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "PlayerRegistration",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "RuleSet",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "AwardDefinition",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "Coach",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "SourceSnapshot",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "Player",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "Match",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "SourcePlayer",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "ImportRun",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "CompetitionRound",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "TeamEntry",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "CompetitionPhase",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "MediaAsset",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "ChampionshipEdition",
                schema: "viw");

            migrationBuilder.DropTable(
                name: "Competition",
                schema: "viw");
        }
    }
}
