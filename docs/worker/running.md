# Executar a importação no Windows

## Preparação

Instale o SDK indicado em `backend/global.json` e disponibilize SQL Server. Não há criação automática do banco ao iniciar o Worker. O usuário da conexão das migrations precisa de permissão para criar/alterar o banco; prefira uma credencial de execução com permissões menores depois da preparação.

No PowerShell, na pasta `backend`:

```powershell
dotnet tool restore
$env:ConnectionStrings__ViwDatabase = "Server=localhost;Database=ViwDevelopment;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
dotnet ef database update --project src/Viw.Infrastructure
# Executar uma carga e encerrar:
dotnet run --project src/Viw.Worker -- --Imports:RunOnce=true
# Executar continuamente, imediatamente e uma hora após o fim de cada ciclo:
dotnet run --project src/Viw.Worker
```

`TrustServerCertificate=True` é somente um exemplo para desenvolvimento local. Configure certificado válido na produção. Senhas ficam em variáveis de ambiente ou User Secrets, nunca no repositório.

## Catálogo e agendamento

`backend/src/Viw.Worker/championships.json` contém as oito edições e seus identificadores externos. Inclui copas encerradas e a La Liga em andamento. Os IDs não ficam no README. O banco mantém a correlação com a origem necessária para atualizar sem duplicar.

Todas as edições habilitadas são verificadas em cada ciclo, inclusive as encerradas, para detectar correções. `Imports:PollIntervalSeconds` vale 3600 por padrão. Não há ciclos simultâneos no mesmo processo; leases com expiração e verificação em cada transação coordenam processos distintos. Uma lease perdida interrompe as gravações desse executor. Cancelamentos encerram o lote; execuções abandonadas são registradas e recuperadas após a expiração.

Para conferir apenas as fontes principais sem banco, upload ou gravação:

```powershell
dotnet run --project src/Viw.Worker -- --Imports:ValidateSourceOnly=true --Imports:RunOnce=true
```

Esse modo não valida súmulas nem permissões de escrita no SQL Server/storage. A saída de uma carga única é diferente de zero quando há falhas.

## Persistência e recuperação

Os quatro recursos principais são validados antes de gravar e persistidos em uma transação. Cada súmula tem transação e checkpoint próprios. Uma falha de súmula não apaga as demais e será tentada no próximo ciclo, mesmo sem mudança nos dados principais. Snapshots preservam JSON e hash; checkpoints incluem a versão do mapeamento e a revisão da partida. Correções atualizam os fatos e reconstruem as estatísticas derivadas da súmula.

Não há exclusão automática de equipes, jogadores ou partidas por ausência em uma resposta. Eventos retirados de uma súmula válida são marcados como removidos e suas estatísticas são recalculadas. Respostas incompletas ou inválidas preservam o último lote válido. Mensagens são gravadas na Outbox na mesma transação; o consumidor de mensagens será implementado posteriormente. As consultas atuais usam o banco diretamente.

Jogadores recebem identidade inicial própria; nomes semelhantes nunca são unidos automaticamente. Nome canônico, vínculos administrativos, premiações, técnicos, craques e imagens oficiais não são substituídos pelo importador. As estatísticas por jogador usam os vínculos ativos e mantêm a indicação de súmulas incompletas e presenças desconhecidas. Não são atribuídos pontos ou critérios de desempate sem regras cadastradas.

## Placar e estatísticas

Um lado `null` dentro do objeto de placar de uma partida em andamento/encerrada significa zero. Uma partida futura ou em andamento sem objeto de placar permanece sem resultado. Quando a partida está encerrada (`st=3` ou timestamp `finished` positivo), `dt` nulo ou ausente representa **0 × 0**, conforme confirmado pelo responsável com as telas de Liverpool × Wolves (Premier League 2024, 20/04/2024) e Coritiba × Flamengo (Copa do Brasil 2023, 30/09/2023). Objetos de placar com tipos inválidos continuam rejeitados. Os snapshots preservam o valor original; o mapeamento `copafacil-v2` força a revisão dos checkpoints anteriores no próximo ciclo.

Gols e cartões são derivados dos eventos observados. Agregados da fonte também são preservados separadamente. Presença não é inferida apenas por jogador inscrito ou gol. O significado de reserva como ausência é habilitado apenas na edição cujo contrato foi conferido. Estatística calculada não concede automaticamente artilheiro oficial nem Ballon d’Or; essas decisões continuam administrativas.

## Fotos

`Media:Driver=Local` grava arquivos em `data/media` (volume persistente no Docker). `Media:Driver=R2` usa storage compatível com S3: configure `Media__R2Endpoint`, `Media__Bucket`, `Media__AccessKeyId` e `Media__SecretAccessKey` no ambiente. Não há provisionamento de conta/bucket ou garantia de gratuidade. A aplicação não torna o bucket público automaticamente.

Downloads aceitam somente HTTPS e hosts permitidos, sem redirecionamentos, com limites de tempo/tamanho e validação de assinatura. Arquivos são endereçados por hash e nunca substituem fotos oficiais. Falhas mantêm a última referência válida e serão tentadas novamente. Upload seguido de falha no banco pode deixar objeto órfão; a limpeza será tratada posteriormente.

## Docker e testes

Aplique as migrations pelo serviço `migrations` antes de habilitar `VIW_IMPORTS_ENABLED=true` no Compose. O volume `worker-media` preserva os arquivos locais. Consulte o Compose e o guia de banco para a conexão.

```powershell
dotnet test Viw.FuncionalSoccer.slnx --configuration Release
```

Os testes SQL criam um banco descartável por teste quando `ConnectionStrings__ViwTestServer` está configurado. Sem essa variável, são ignorados explicitamente. Incluem repetição sem duplicação, correção de placar e recuperação de súmula. As validações SQL e a importação real devem ser executadas no ambiente Windows; não foram executadas contra um SQL Server nesta entrega.
