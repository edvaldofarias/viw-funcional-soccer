# Desenvolvimento local

## Disponível nesta etapa

Solução `backend/Viw.FuncionalSoccer.slnx` com oito projetos .NET 10: quatro bibliotecas, Worker, Web API e dois projetos de teste. O MVC é um projeto separado em `frontend`. O Worker apenas inicializa e respeita cancelamento; não consulta Copa Fácil nem importa dados. Habilitar `Imports:Enabled=true` falha explicitamente para evitar uma falsa execução de importação.

A API expõe `/api/v1/system`, `/health/live` e, em Development, `/openapi/v1.json`. MVC consulta a API no servidor. Se ela estiver indisponível, a página retorna HTTP 503. Liveness não verifica banco e não significa que a importação esteja saudável.

## Requisitos

SDK .NET 10.0.301 ou posterior da linha 10.0 (bandas de recursos mais novas permitidas por `rollForward: latestFeature` em `backend/global.json` e `frontend/global.json`; versões preview não são aceitas) ou Docker com serviço ativo. Banco não é necessário para iniciar esta fundação. Node/Angular só serão necessários na etapa do Admin.

## Comandos na raiz

```sh
dotnet restore backend/Viw.FuncionalSoccer.slnx
dotnet build backend/Viw.FuncionalSoccer.slnx --configuration Release
dotnet test backend/Viw.FuncionalSoccer.slnx --configuration Release
dotnet build frontend/mvc/Viw.Mvc/Viw.Mvc.csproj --configuration Release
```

Execute os hosts em terminais separados:

```sh
dotnet run --project backend/src/Viw.WebApi
dotnet run --project frontend/mvc/Viw.Mvc
dotnet run --project backend/src/Viw.Worker
```

API: `http://localhost:5080`; MVC: `http://localhost:5081`. Para executar o Worker, primeiro configure a conexão e aplique as migrations. Consulte [o guia do Worker](../worker/running.md). Para conferir apenas a fonte sem banco: `dotnet run --project backend/src/Viw.Worker -- --Imports:ValidateSourceOnly=true --Imports:RunOnce=true`.

## Docker

```sh
docker compose config --quiet
docker compose up --build webapi mvc worker
docker compose --profile tests run --build --rm tests
docker compose down
```

Os hosts .NET têm Dockerfiles próprios e executam como usuário sem privilégios. As portas são vinculadas ao localhost. O Compose usa Development somente para uso local; não é uma configuração pronta de produção. MVC usa `http://webapi:8080/` na rede Docker. `depends_on` ordena início, não garante prontidão; o MVC trata indisponibilidade e pode ser atualizado após a API iniciar.

O perfil opcional de banco exige senha local forte em `.env`:

```sh
cp .env.example .env
# Preencha SQLSERVER_SA_PASSWORD antes de executar.
docker compose --profile sqlserver up -d sqlserver
```

Não há `platform: linux/amd64` forçando emulação no Mac ARM. A imagem SQL Server precisa de host compatível; banco remoto pode ser usado no lugar do container. `docker compose down` preserva o volume do banco; não usar `down -v` sem intenção de apagar os dados locais.

## Configurações

- Worker: `Imports__Enabled=false`, `Imports__RunOnce`, `Imports__PollIntervalSeconds` (5 a 86400; intervalo após cada ciclo).
- MVC: `ViwApi__BaseUrl` absoluta HTTP/HTTPS.
- Backend: `ConnectionStrings__ViwDatabase` opcional nesta fundação e obrigatória para migrations.

Use ambiente ou User Secrets dos hosts para segredos. Não versionar `.env`, tokens, dumps, logs ou snapshots pessoais. Consulte [banco e migrations](database.md) para a configuração da ferramenta design-time.

## Testes

Unidade: limites das opções de execução. Integração: pipeline HTTP real via WebApplicationFactory, contrato público de estado, liveness e 404. Há testes do modelo/migrations e oito cenários com SQL Server real isolado, habilitado por `ConnectionStrings__ViwTestServer`. Sem servidor, esses cenários são explicitamente ignorados. Não usar produção para testes.

A CI executa restore, build, testes e build dos containers; não publica aplicações nem aplica migrations automaticamente.

## Validação desta entrega

Compilação Release sem avisos; 12 casos de unidade e nove de integração aprovados; oito cenários SQL Server explicitamente ignorados sem conexão de testes. Verificados também Worker em execução única, MVC consumindo API (HTTP 200), indisponibilidade da API (HTTP 503), sintaxe do Compose, geração de SQL idempotente e ausência de mudanças pendentes no modelo EF.

O daemon Docker não estava ativo no ambiente desta entrega: build e execução das imagens, subida do SQL Server e aplicação real da migration ainda precisam ser validados com Docker ativo ou banco de desenvolvimento disponível. A geração de SQL não comprova conexão, permissões nem aplicação em SQL Server.

As configurações e a ferramenta de migrations do backend ficam em `backend/`. O frontend tem seus próprios `global.json` e `Directory.Build.props`; MVC referencia apenas os contratos compartilhados. Compose, ignore de Docker e CI ficam na raiz porque coordenam ambas as aplicações.

Organização dos cenários, AAA e uso de Moq: [convenções de testes](testing.md).
