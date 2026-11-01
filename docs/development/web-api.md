# Web API de consultas — v1

A API lê o SQL Server preenchido pelo Worker. Não consulta o Firebase durante requisições, não inicia importações e não altera dados. Não aplica migrations ao iniciar.

## Executar

Na pasta `backend`, configure `ConnectionStrings__ViwDatabase` ou User Secrets do projeto `src/Viw.WebApi` com a mesma base utilizada pelo Worker. Aplique as migrations pelo fluxo já documentado e execute:

```powershell
dotnet run --project src/Viw.WebApi -- --urls http://localhost:5080
```

O documento OpenAPI fica em `http://localhost:5080/openapi/v1.json` no ambiente Development. É o documento JSON, sem dependência de Swagger UI. Em produção ele não é exposto. Exemplos também estão em `backend/src/Viw.WebApi/Viw.WebApi.http`.

Sem conexão configurada, a API inicia para permitir o diagnóstico: as consultas de dados respondem `503` com ProblemDetails, sem expor detalhes da conexão. `/health/live` verifica apenas o processo; `/health/ready` responde `200` quando o banco aceita conexão e `503` quando indisponível. Readiness não verifica se todas as migrations foram aplicadas; elas continuam sendo uma etapa explícita antes de liberar tráfego.

## Consultas públicas

As rotas abaixo usam `/api/v1` como prefixo. `{slug}` identifica uma edição cadastrada, por exemplo `la-liga-2026-2`; IDs de equipe/partida são os GUIDs retornados pela API, não IDs do Firebase.

| GET | Conteúdo / filtros adicionais |
| --- | --- |
| `/championships` | Edições; filtro `year` |
| `/championships/{slug}` | Edição, fases, rodadas e contagens |
| `/championships/{slug}/teams` | Equipes da edição |
| `/championships/{slug}/teams/{teamId}` | Equipe e designações de técnicos |
| `/championships/{slug}/teams/{teamId}/players` | Inscrições observadas; nomes oficiais e originais |
| `/championships/{slug}/matches` | Partidas; filtros `teamId`, `phaseId`, `roundId`, `status` |
| `/championships/{slug}/matches/{matchId}` | Partida e placar; participantes futuros podem estar indefinidos |
| `/championships/{slug}/matches/{matchId}/players` | Estatísticas individuais registradas na súmula |
| `/championships/{slug}/statistics/players` | Totais individuais, ordenados por gols, nome e ID |
| `/championships/{slug}/statistics/teams` | Jogos, vitórias, empates, derrotas, gols pró/contra; ordem alfabética |
| `/championships/{slug}/awards` | Somente premiações oficiais confirmadas, incluindo craques quando vinculados à partida |
| `/players` | Identidades canônicas; filtros `search` e `editionId` |
| `/players/{slug}` | Identidade oficial e totais da carreira |
| `/players/{slug}/history` | Edições, equipes e nomes originais associados pelos vínculos ativos |

Estados de partida aceitos: `Scheduled`, `Live`, `Finished`, `Cancelled`, `Unknown`. Valores inválidos recebem `400`. Listas aceitam `page` (padrão 1, máximo 100000) e `pageSize` (padrão 20, máximo 100). O resultado contém `items`, `page`, `pageSize` e `totalCount`. Ordenações incluem um ID como desempate para paginação estável; consultas grandes não são carregadas integralmente antes de paginar.

Recursos inexistentes ou pertencentes a outra edição recebem `404`. Uma lista vazia em um recurso existente recebe `200`, sem confundir ausência de dados com edição inexistente. Erros de consulta usam ProblemDetails; uma indisponibilidade do banco não retorna lista vazia com sucesso. O identificador de rastreio permite correlação com os logs.

## Estatísticas, identidades e dados oficiais

- Totais de carreira e da edição consideram fatos de partidas encerradas. Estatísticas em andamento podem ser consultadas na própria partida.
- Vínculos ativos relacionam o cadastro da origem à identidade canônica. A busca considera o nome oficial, nomes originais e apelidos vinculados. A mesma identidade não aparece duplicada na busca por ter vários cadastros na fonte.
- Presenças desconhecidas não são convertidas em presença/ausência confirmada. `knownAppearances`, `unknownAppearances` e `incompleteSheets` tornam essa diferença explícita. Uma súmula com revisão anterior à partida também é sinalizada como incompleta.
- Totais são os fatos disponíveis nas súmulas; não são silenciosamente substituídos por agregados textuais da fonte. Sem súmulas, eles podem estar incompletos ou vazios e devem ser exibidos com essa ressalva no futuro MVC.
- Inscrições contêm períodos **observados pelo importador**; não afirmam a data histórica exata de uma transferência. Técnicos retornam seus períodos e eventual rodada; não são inferidos a partir de jogadores.
- Não há classificação por pontos nem critérios de desempate presumidos. A consulta de equipes retorna totais, sem atribuir posições de campeonato. A definição e execução das regras confirmadas será uma etapa posterior.
- Artilharia calculada não concede prêmio oficial. Ballon d’Or, artilheiro oficial, craques e seleções são lidos das concessões confirmadas. Rascunhos e concessões revogadas não são públicos.
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
