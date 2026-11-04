# Web API de consultas — v1

A API lê o SQL Server preenchido pelo Worker. Não consulta o Firebase durante requisições, não inicia importações e não altera dados. Não aplica migrations ao iniciar.

## Executar

Na pasta `backend`, configure `ConnectionStrings__ViwDatabase` ou User Secrets do projeto `src/Viw.WebApi` com a mesma base utilizada pelo Worker. Aplique as migrations pelo fluxo já documentado e execute:

```powershell
dotnet run --project src/Viw.WebApi -- --urls http://localhost:5080
```

O documento OpenAPI fica em `http://localhost:5080/openapi/v1.json` no ambiente Development. É o documento JSON, sem dependência de Swagger UI. Em produção ele não é exposto. Exemplos também estão em `backend/src/Viw.WebApi/Viw.WebApi.http`.

Sem conexão configurada, a API inicia para permitir o diagnóstico: as consultas de dados respondem `503` com ProblemDetails, sem expor detalhes da conexão. `/health/live` verifica apenas o processo; `/health/ready` responde `200` quando o banco aceita conexão e `503` quando indisponível. Readiness não verifica se todas as migrations foram aplicadas; elas continuam sendo uma etapa explícita antes de liberar tráfego.

## Comentários para Swagger/OpenAPI

Os controllers e contratos públicos têm comentários XML em português: resumo, comportamento, parâmetros, limites, significado dos campos retornados e códigos de resposta. `GenerateDocumentationFile` está habilitado nos projetos `Viw.WebApi` e `Viw.Contracts`; o gerador nativo do .NET 10 incorpora essa documentação ao documento OpenAPI. O Swagger UI poderá consumir o mesmo `/openapi/v1.json` quando sua interface for adicionada.

Os filtros agrupados em objetos `[FromQuery]` reutilizam as descrições XML dos schemas por meio de `QueryParameterDocumentationTransformer`. Mantenha a explicação na propriedade do contrato, sem duplicar o texto em um mapa manual. As respostas de sucesso têm tipo declarado com `ProducesResponseType`; respostas de erro também têm metadados, pois um comentário `response` sozinho não declara o código HTTP.

Ao criar uma rota, documente `summary`, `remarks`, cada `param`, `returns` e os `response` aplicáveis. Documente os campos dos records por seus parâmetros e as propriedades dos filtros por `summary`. Não use exemplos com credenciais reais. Os testes de documentação verificam o documento gerado, incluindo descrições de parâmetros, schemas e códigos de erro.

## Consultas públicas

As rotas abaixo usam `/api/v1` como prefixo. `{slug}` identifica uma edição cadastrada, por exemplo `la-liga-2026-2`; IDs de equipe/partida são os GUIDs retornados pela API, não IDs do Firebase.

| GET | Conteúdo / filtros adicionais |
| --- | --- |
| `/championships` | Edições; filtro `year` |
| `/championships/{slug}` | Edição, fases, rodadas e contagens |
| `/championships/{slug}/teams` | Equipes da edição |
| `/championships/{slug}/teams/{teamId}` | Equipe e designações de técnicos |
| `/championships/{slug}/teams/{teamId}/players` | Inscrições observadas; nomes oficiais e originais |
| `/championships/{slug}/teams/{teamId}/players/{sourcePlayerId}` | Modal do cadastro na edição com contexto de equipe |
| `/championships/{slug}/players/{sourcePlayerId}` | Mesmo modal sem contexto de equipe; Wiki opcional somente dentro dos detalhes |
| `/championships/{slug}/matches` | Partidas; filtros `teamId`, `phaseId`, `roundId`, `status` |
| `/championships/{slug}/matches/{matchId}` | Partida e placar; participantes futuros podem estar indefinidos |
| `/championships/{slug}/matches/{matchId}/players` | Estatísticas individuais registradas na súmula |
| `/championships/{slug}/statistics/players` | Totais individuais, ordenados por gols, nome e ID |
| `/championships/{slug}/statistics/teams` | Jogos, vitórias, empates, derrotas, gols pró/contra; ordem alfabética |
| `/championships/{slug}/awards` | Premiações editoriais confirmadas e reconhecimento automático dos artilheiros de edições encerradas |
| `/players` | Identidades canônicas; filtros `search` e `editionId` |
| `/players/{slug}` | Identidade oficial e totais da carreira |
| `/players/{slug}/history` | Edições, equipes e nomes originais associados pelos vínculos ativos |

Estados de partida aceitos: `Scheduled`, `Live`, `Finished`, `Cancelled`, `Unknown`. Valores inválidos recebem `400`. Listas aceitam `page` (padrão 1, máximo 100000) e `pageSize` (padrão 20, máximo 100). O resultado contém `items`, `page`, `pageSize` e `totalCount`. Ordenações incluem um ID como desempate para paginação estável; consultas grandes não são carregadas integralmente antes de paginar.

Recursos inexistentes ou pertencentes a outra edição recebem `404`. Uma lista vazia em um recurso existente recebe `200`, sem confundir ausência de dados com edição inexistente. Erros de consulta usam ProblemDetails; uma indisponibilidade do banco não retorna lista vazia com sucesso. O identificador de rastreio permite correlação com os logs.

## Estatísticas, identidades e dados oficiais

- Totais de carreira e da edição consideram fatos de partidas encerradas. Estatísticas em andamento podem ser consultadas na própria partida.
- Vínculos ativos relacionam o cadastro da origem à identidade canônica. A busca considera o nome oficial, nomes originais e apelidos vinculados. A mesma identidade não aparece duplicada na busca por ter vários cadastros na fonte.
- As linhas de `/championships/{slug}/matches/{matchId}/players` incluem `sourcePlayerId` (cadastro da edição) além de `playerId` (identidade canônica opcional). O primeiro permite obter a foto por `/api/media/sourceplayers/{sourcePlayerId}` mesmo sem vínculo com a Wiki. `teamId` vem do fato da partida e permite separar as equipes sem consultar ou inferir o elenco atual.
- Presenças desconhecidas não são convertidas em presença/ausência confirmada. `knownAppearances`, `unknownAppearances` e `incompleteSheets` tornam essa diferença explícita. Uma súmula com revisão anterior à partida também é sinalizada como incompleta.
- Totais são os fatos disponíveis nas súmulas; não são silenciosamente substituídos por agregados textuais da fonte. Sem súmulas, eles podem estar incompletos ou vazios e devem ser exibidos com essa ressalva no futuro MVC.
- Inscrições contêm períodos **observados pelo importador**; não afirmam a data histórica exata de uma transferência. Técnicos retornam seus períodos e eventual rodada; não são inferidos a partir de jogadores.
- O elenco (`/championships/{slug}/teams/{teamId}/players`) inclui `statistics` de partidas encerradas da equipe/edição e `discipline` da edição inteira. Cartões seguem `PlayerDisciplineReader`, incluindo agregados históricos em edições arquivadas; em transferências, não são atribuídos artificialmente a uma equipe. Agregações são feitas após paginar, em lote, mesmo para cadastros sem Wiki. Datas observadas permanecem no contrato, mas o estado técnico aberto/encerrado não deve ser exibido. `Player.Name` e `OriginalName` preservam o nome original; apenas o slug indica a identidade canônica vinculada.
- O detalhe do jogador valida edição e `sourcePlayerId` (não ID canônico). A rota com equipe aceita inscrição ou fato de participação naquela equipe; a rota sem equipe resolve a inscrição observada mais recente, senão a participação mais recente por data/ID. Sem equipe conhecida, `Team` e `FinalPosition` são null, sem impedir detalhes de cadastros premiados sem jogos. Ambas usam `EditionPlayerAsync` e retornam `EditionPlayerResponse`: `Player` preserva ID/nome/foto da edição e slug vazio; `WikiSlug` vem apenas do vínculo canônico ativo e permite navegar opcionalmente para a carreira de dentro do modal. Sem vínculo, é null, sem heurística por nome.
- Os detalhes incluem totais da edição inteira, resultados de jogos com presença confirmada, artilharia por gols com empates, colocação final da equipe somente quando arquivada e conhecida, e reconhecimentos do cadastro/edição. Artilharia sem gols é null. Todos os líderes empatados com gols de edição encerrada recebem o reconhecimento de artilheiro. Cartões reutilizam `PlayerDisciplineReader` e colocação reutiliza `FinalPlacementsAsync`; partidas em andamento e outros cadastros/edições não entram nos totais pessoais. Recurso fora do escopo retorna 404. Não há cálculo de MVP ou consulta às estatísticas da carreira.
- Não há classificação por pontos nem critérios de desempate presumidos. A consulta de equipes retorna totais, sem atribuir posições de campeonato. A definição e execução das regras confirmadas será uma etapa posterior.
- Artilharia de edição `Archived` reconhece todos os líderes empatados no maior total positivo de gols das partidas encerradas, independentemente de identidade na Wiki. A regra compartilhada está em `ViwQueries.Awards.cs`: um reconhecimento por edição/cadastro, transferências somadas, concessões editoriais válidas reutilizadas e antigas concessões de não líderes omitidas da leitura pública, sem alterar o Admin. Os demais prêmios exigem concessões confirmadas; rascunhos/revogações não são publicados. Reconhecimentos derivados retornam `automatic=true`, ID determinístico e `confirmedAt=null`; não gravam concessões nem inventam data de confirmação. Correções de gols e reabertura da edição se refletem imediatamente. Sem gols registrados não há reconhecimento automático.
- A paginação de premiações mantém as concessões editoriais no banco (data decrescente/ID), depois os artilheiros da edição encerrada (nome/ID do cadastro). `totalCount` inclui ambos e não duplica artilheiros com concessão válida.
- Fotos retornam `imageAssetId`/`coverAssetId`, preferindo o arquivo oficial quando existente. Os contratos não expõem URLs de origem com tokens, chaves privadas do storage nem arquivos privados. A entrega de imagens por URL pública/autorizada será integrada à publicação do storage e ao MVC; esta etapa não torna buckets ou arquivos públicos.

## Acompanhamento autorizado do Worker

Configure `Monitoring__ApiKey` fora do repositório: uma chave aleatória de 32 a 512 caracteres. Sem chave configurada, o monitoramento permanece fechado. No Docker Compose de desenvolvimento, a variável correspondente é `VIW_MONITORING_API_KEY`.

Envie a chave somente no header `X-Viw-Monitor-Key`. Não a coloque na URL, no Angular, no HTML ou no JavaScript do navegador. Essa credencial serve exclusivamente para leitura servidor-a-servidor; não dá permissão de iniciar carga, alterar identidades ou administrar prêmios. Não substitui o futuro login do Admin.

| GET | Conteúdo |
| --- | --- |
| `/imports/runs` | Execuções paginadas; filtro opcional `editionId` |
| `/imports/runs/{id}` | Estado, contagens, horários, código de falha, lease ativa e indicação de recuperação pendente |
| `/imports/editions/{editionId}/checkpoints` | Últimos checkpoints da edição, paginados, com recurso e horário |

Os checkpoints são o estado atual da **edição**, não um histórico por execução. O indicador de lease é uma leitura operacional do estado atual; não libera nem toma a lease. Não há porcentagem fictícia de progresso: a resposta mostra os recursos confirmados e as contagens registradas. Respostas de monitoramento usam `Cache-Control: no-store`; credenciais ausentes ou inválidas recebem `401`.

HTTPS é obrigatório para autenticar esse monitoramento fora de Development. Atrás de proxy, configure apenas os proxies/redes conhecidos para encaminhar o protocolo original; cabeçalhos de remetentes não confiáveis não devem ser aceitos. Não desabilite essa verificação para publicar. No futuro MVC, a página de operação também deverá autorizar o usuário antes de consultar a API com a credencial do servidor.

## Arquitetura e verificação

`Viw.Contracts` define os contratos; `Viw.Application/Queries/IViwQueries` define as consultas; `Viw.Infrastructure/Queries` implementa as leituras EF Core sem tracking; controllers tratam o HTTP. O Worker continua sendo o responsável pela sincronização. Não foram adicionadas entidades ou migrations nesta etapa.

Os testes HTTP usam Moq para isolar o protocolo e validar paginação, respostas de erro, OpenAPI e autorização. Os testes de consulta SQL criam bancos descartáveis via migrations e verificam isolamento de edições, histórico/aliases, vínculos ativos, estatísticas, premiações e monitoramento. São executados na CI quando `ConnectionStrings__ViwTestServer` está definido. Sem servidor local, esses testes são explicitamente ignorados; isso não equivale a validar a consulta no SQL Server.

A implantação real, a carga dos campeonatos e as verificações SQL permanecem necessárias no Windows/CI antes de usar os dados em produção. O Docker Compose foi preparado para receber a conexão da API e a credencial de monitoramento por ambiente.

### Resolução de identidade para o portal

`GET /api/v1/players/by-id/{id}` retorna o jogador canônico e seu slug atual; 404 se ausente. Usado pelo link permanente da Wiki sem deduzir slugs a partir dos nomes.

## Visão consolidada da edição

`GET /api/v1/championships/{slug}/overview` retorna fases com jogos e classificações, além da classificação geral por avanço na competição. Posição e pontos da fase são preservados da fonte; são nulos quando indisponíveis. Estatísticas coletivas consideram apenas partidas encerradas com equipes e placar completos. Cada fase inclui `groups` com as classificações dos grupos configurados em `PhaseGroups` ou inferidos automaticamente apenas na primeira fase. Mata-mata não gera grupos automaticamente; pontos corridos mantêm tabela única. Participantes são resolvidos por `CompetitionGroupTeam`, vinculando edição, fase e equipe, sem sobrescrita por fases posteriores. Dentro do grupo a posição é relativa ao grupo, ordenada por pontos oficiais e posição global da fonte no desempate. A operação e seus contratos têm comentários XML para a documentação OpenAPI/Swagger.

Os contratos de partidas incluem `homePenalties` e `awayPenalties` opcionais. Null significa valor não informado; zero é um placar válido. Pênaltis não compõem gols pró/contra.
