# Banco de dados e migrations

A solução usa EF Core 10 com SQL Server e migrations versionadas em `backend/src/Viw.Infrastructure/Persistence/Migrations`. Não usar `EnsureCreated`, SQL manual para modelar tabelas ou migrations automáticas no startup dos hosts.

A migration `InitialCreate` é a baseline; `CreateCompetitionModel` cria as 25 tabelas iniciais de competição, identidade, premiações e controle de importação. A migration `ApplyDefaultColumnConventions` define o padrão de campos curtos e suas exceções. Consulte [o modelo](../architecture/database-model.md). Nenhum banco é criado ao apenas iniciar API, MVC ou Worker.

## Comandos dentro de `backend`

```sh
cd backend
dotnet tool restore
# Defina esta variável no seu ambiente com uma conexão de desenvolvimento.
export ConnectionStrings__ViwDatabase='Server=localhost,1433;Database=ViwDevelopment;User Id=sa;Password=<senha-local>;Encrypt=True;TrustServerCertificate=True'
dotnet ef database update --project src/Viw.Infrastructure
```

O exemplo usa confiança no certificado apenas para desenvolvimento. Em produção, configurar certificado válido e credencial própria. A variável também é lida pelos hosts. A factory de design-time lê a variável diretamente; não lê User Secrets do Worker.

Para novas alterações de modelo:

```sh
dotnet ef migrations add NomeDaAlteracao --project src/Viw.Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/Viw.Infrastructure --output artifacts/migrations.sql
```

Criar `backend/artifacts/` (ou `artifacts/` a partir de `backend`) antes de exportar o script. Revisar migrations e fazer backup antes de aplicar em produção. Não publicar senhas, scripts com dados pessoais ou dumps no Git.

## Docker

O perfil `sqlserver` é opcional. Defina `SQLSERVER_SA_PASSWORD` em `.env` (use `.env.example` como referência) antes de subir. Requer host compatível com a imagem SQL Server; no Mac ARM use banco remoto compatível ou ambiente x64, sem presumir suporte nativo.

Para aplicar migrations por container, com o banco já disponível:

```sh
# A conexão precisa alcançar o banco a partir da rede Docker, por exemplo Server=sqlserver,1433.
docker compose --profile migrations run --rm migrations
```

Exporte `ConnectionStrings__ViwDatabase` no shell que executa o Compose ou configure em `.env`. Banco e migrações são etapas explícitas; os serviços básicos não dependem de SQL Server nesta primeira entrega.

## Testes reais de banco

Dentro de `backend`, defina `ConnectionStrings__ViwTestServer` com acesso a um servidor exclusivo de testes e permissão para criar/apagar bancos temporários. O teste cria um banco `ViwTests_<guid>` próprio e não usa o catálogo informado na conexão.

```sh
dotnet test Viw.FuncionalSoccer.slnx --configuration Release
```

Sem a variável, o teste SQL é explicitamente ignorado. A CI executa esse teste com SQL Server isolado em runner x64. Os demais testes não exigem servidor. Nunca fornecer credenciais de produção.
