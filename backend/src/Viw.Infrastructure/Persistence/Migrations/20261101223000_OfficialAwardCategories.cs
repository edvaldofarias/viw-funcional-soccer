using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace Viw.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ViwDbContext))]
[Migration("20261101223000_OfficialAwardCategories")]
public sealed class OfficialAwardCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [viw].[AwardDefinition] WHERE [Code]=N'official-top-scorer') INSERT INTO [viw].[AwardDefinition] ([Id],[CreatedAt],[Code],[Name],[Scope],[RecipientKind]) VALUES ('79000000-0000-0000-0000-000000000001',SYSUTCDATETIME(),N'official-top-scorer',N'Artilheiro do campeonato',0,0);");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [viw].[AwardDefinition] WHERE [Code]=N'official-ballon-dor') INSERT INTO [viw].[AwardDefinition] ([Id],[CreatedAt],[Code],[Name],[Scope],[RecipientKind]) VALUES ('79000000-0000-0000-0000-000000000002',SYSUTCDATETIME(),N'official-ballon-dor',N'Ballon d’Or do campeonato',0,0);");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [viw].[AwardDefinition] WHERE [Code]=N'official-match-mvp') INSERT INTO [viw].[AwardDefinition] ([Id],[CreatedAt],[Code],[Name],[Scope],[RecipientKind]) VALUES ('79000000-0000-0000-0000-000000000003',SYSUTCDATETIME(),N'official-match-mvp',N'Craque da partida',2,0);");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [viw].[AwardDefinition] WHERE [Code]=N'selection-championship') INSERT INTO [viw].[AwardDefinition] ([Id],[CreatedAt],[Code],[Name],[Scope],[RecipientKind]) VALUES ('79000000-0000-0000-0000-000000000004',SYSUTCDATETIME(),N'selection-championship',N'Seleção do campeonato',0,0);");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [viw].[AwardDefinition] WHERE [Code]=N'selection-round') INSERT INTO [viw].[AwardDefinition] ([Id],[CreatedAt],[Code],[Name],[Scope],[RecipientKind]) VALUES ('79000000-0000-0000-0000-000000000005',SYSUTCDATETIME(),N'selection-round',N'Seleção da rodada',1,0);");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [viw].[AwardDefinition] WHERE [Code]=N'official-best-coach') INSERT INTO [viw].[AwardDefinition] ([Id],[CreatedAt],[Code],[Name],[Scope],[RecipientKind]) VALUES ('79000000-0000-0000-0000-000000000006',SYSUTCDATETIME(),N'official-best-coach',N'Melhor técnico do campeonato',0,1);");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM [viw].[AwardDefinition] WHERE [Id]='79000000-0000-0000-0000-000000000001' AND NOT EXISTS (SELECT 1 FROM [viw].[AwardGrant] WHERE [AwardDefinitionId]='79000000-0000-0000-0000-000000000001');");
        migrationBuilder.Sql("DELETE FROM [viw].[AwardDefinition] WHERE [Id]='79000000-0000-0000-0000-000000000002' AND NOT EXISTS (SELECT 1 FROM [viw].[AwardGrant] WHERE [AwardDefinitionId]='79000000-0000-0000-0000-000000000002');");
        migrationBuilder.Sql("DELETE FROM [viw].[AwardDefinition] WHERE [Id]='79000000-0000-0000-0000-000000000003' AND NOT EXISTS (SELECT 1 FROM [viw].[AwardGrant] WHERE [AwardDefinitionId]='79000000-0000-0000-0000-000000000003');");
        migrationBuilder.Sql("DELETE FROM [viw].[AwardDefinition] WHERE [Id]='79000000-0000-0000-0000-000000000004' AND NOT EXISTS (SELECT 1 FROM [viw].[AwardGrant] WHERE [AwardDefinitionId]='79000000-0000-0000-0000-000000000004');");
        migrationBuilder.Sql("DELETE FROM [viw].[AwardDefinition] WHERE [Id]='79000000-0000-0000-0000-000000000005' AND NOT EXISTS (SELECT 1 FROM [viw].[AwardGrant] WHERE [AwardDefinitionId]='79000000-0000-0000-0000-000000000005');");
        migrationBuilder.Sql("DELETE FROM [viw].[AwardDefinition] WHERE [Id]='79000000-0000-0000-0000-000000000006' AND NOT EXISTS (SELECT 1 FROM [viw].[AwardGrant] WHERE [AwardDefinitionId]='79000000-0000-0000-0000-000000000006');");
    }
}
