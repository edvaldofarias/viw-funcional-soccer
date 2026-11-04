using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viw.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImportOfficialResultOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceResultOrder",
                schema: "viw",
                table: "TeamEntry",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeamEntry_SourceResultOrder",
                schema: "viw",
                table: "TeamEntry",
                sql: "[SourceResultOrder] IS NULL OR [SourceResultOrder] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TeamEntry_SourceResultOrder",
                schema: "viw",
                table: "TeamEntry");

            migrationBuilder.DropColumn(
                name: "SourceResultOrder",
                schema: "viw",
                table: "TeamEntry");
        }
    }
}
