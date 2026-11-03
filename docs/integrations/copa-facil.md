# Copa Fácil — contrato observado v1

Inspeção realizada em 01/11/2026, com consultas somente de leitura a uma edição histórica (Copa do Nordeste 2023/1) e à La Liga 2026/2. Não generalizar automaticamente os resultados para todas as outras edições. IDs externos reais, nomes pessoais e URLs de fotos não estão reproduzidos nesta documentação.

Base consultada:

```text
https://copafacil-web.firebaseio.com/events/{externalChampionshipId}/
```

## Recursos confirmados

| Caminho | Formato observado | Histórico | La Liga |
| --- | --- | --- | --- |
| `info.json` | Objeto de metadados | 12 campos de topo | 17 campos de topo |
| `matchs.json` | Objeto indexado pelo ID da partida | 26 partidas, todas `st=3` | 43 registros; 30 `st=3`, 13 sem `st` |
| `matches.json` | `null` nas duas consultas | Sem recurso | Sem recurso |
| `teams.json` | Objeto indexado pelo ID da equipe | 8 equipes | 12 equipes |
| `player.json` | Objeto indexado pelo ID do cadastro | 99 cadastros | 156 cadastros |
| `details/{matchId}.json` | Súmula com `info`, `list` e eventualmente `clock` | Duas súmulas inspecionadas | Duas súmulas inspecionadas |

Contagens representam o instante da consulta, não limites, asserções permanentes ou totais de importação. `null` em um recurso inteiro significa ausência/indisponibilidade, não autorização para apagar dados.

## Mapeamento

| Origem | Destino e interpretação |
| --- | --- |
| Chave de cada objeto | ID externo textual, exato e sensível a maiúsculas; nunca chave baseada no nome |
| `info.info.title` | Nome recebido da edição; nome/slug oficial podem ser administrados |
| `info.d_i`, `info.d_f` | Datas brutas da edição; validar significado antes de usá-las como calendário oficial |
| `info.url`, `info.url_c` | Referências de mídia; guardar origem, não presumir URL permanente |
| `teams.*.name` | `TeamEntry.Name` |
| `teams.*.url` | Referência de imagem original, separada da imagem oficial |
| `teams.*.dt` | Estatísticas agrupadas por chave da fonte; não substituir o cálculo por partida sem validar o contrato |
| `player.*.name`, `nickname` | Nome original e apelido; não unificar pessoas automaticamente |
| `player.*.team` | Equipe atual declarada; não reescrever participações antigas após transferência |
| `player.*.n`, `p` | Número/posição quando disponíveis, preservados como texto |
| `player.*.dt` | Estatísticas acumuladas em texto `chave=valor#...`; não são um documento JSON nem fatos individuais por partida |
| `player.*.dt` chaves `14`, `16` | Totais de cartões amarelos e vermelhos, respectivamente. Usados em edições arquivadas; não somar aos eventos das súmulas |
| `player.*.fs` | Agregados por fase, observados na La Liga; contrato a validar por edição |
| `ART`, `CAR` | Campos auxiliares numéricos; aparecem como inteiros em 2023 e decimais em 2026. Sem significado universal confirmado, não usar como gols/cartões |
| `SUSP`, `SUSP2`, `id_susp` | Metadados de suspensão observados na La Liga; validar regras específicas antes de derivar elegibilidade |
| `matchs.*.team1`, `team2` | Referências de equipes; guardar também as referências brutas quando não resolvidas |
| `matchs.*.fs`, `m_set` | Referências opacas de fase/conjunto. Não converter `m_set` diretamente em número de rodada |
| `matchs.*.title` | Título original; na La Liga há rótulos de quadra (`Q1`), não necessariamente número de rodada |
| `matchs.*.d_i`, `gmt` | Timestamp e ajuste temporal brutos em milissegundos; preservar valores e validar convenção de sinal antes de converter; evitar aplicar offset duas vezes |
| `matchs.*.st` | `3` foi observado em jogos encerrados; valores ausentes/desconhecidos precisam de política explícita |
| `matchs.*.finished` | Na La Liga é timestamp numérico em ms, não booleano. Ausente nos jogos históricos encerrados |
| `matchs.*.dt.qt_g1`, `qt_g2` | Placar por lado; ver regra de zero abaixo |
| `matchs.*.l` | Local recebido, quando presente |
| `details.*.list` | Eventos por chave textual, não uma lista sequencial |
| Evento `ac`, `pl_id1`, `team1`, `val1` | Código, cadastro do jogador, equipe e valor opcionais; preservar JSON original e referências não resolvidas |

`qc`, `qm`, `pos_eli`, `clock` e campos desconhecidos permanecem nos snapshots. Não inventar uma semântica para preencher colunas obrigatórias.

## Regra de placar confirmada pelo responsável

Em resultado registrado, **`null × 3` representa `0 × 3`**. O mesmo vale para o outro lado. Campo de gols omitido em resultado conhecido recebe o mesmo tratamento. A função de domínio `MatchScore.FromRecordedResult` normaliza os lados nulos; nunca deve ser usada para criar placar de um jogo ainda não iniciado.

- Resultado registrado e lado nulo: zero.
- Resultado registrado com ambos os lados nulos: ambos normalizados a zero, quando o adaptador já confirmou que se trata de resultado.
- Partida futura sem resultado: `HomeScore`/`AwayScore` continuam `null`.
- Valor negativo ou inválido: rejeitar/quarentenar; não converter para zero.
- Snapshot original continua intacto, inclusive campos omitidos e nulos explícitos.

O adaptador do Worker usa o estado da partida: se encerrada (`st=3` ou `finished` positivo), o objeto de placar nulo/ausente representa 0 × 0, conforme confirmado pelo responsável nas telas de Liverpool × Wolves e Coritiba × Flamengo. Em partidas futuras ou em andamento sem objeto de placar, o resultado permanece desconhecido. A confirmação humana complementa o contrato observado; o site antigo não é alterado.

## Diferenças e lacunas verificadas

- La Liga contém sete partidas com referências que não resolvem equipes cadastradas; preservar como confrontos futuros/pendentes, sem fabricar times. O modelo permite participantes nulos antes da conclusão.
- Todas as referências de equipe nos cadastros de jogadores resolviam na amostra, mas isso não garante integridade futura.
- Nas súmulas históricas amostradas foram vistos códigos `ac=1` e `9`; na La Liga, `1`, `3`, `4`, `7` e `9`.
- Conferência em 02/11/2026: a Premier League possui totais `14=78` e `16=8` nos cadastros, mas nenhuma ocorrência `ac=3/4` nas súmulas. Na La Liga, os 53 amarelos (`ac=3`) e seis vermelhos (`ac=4`) correspondem aos agregados `14/16`. Por isso, edições arquivadas usam os totais por cadastro quando a chave está presente, inclusive zero explícito; sem a chave, usam eventos de partidas encerradas. Edições em andamento continuam usando somente eventos de partidas encerradas. Agregados não são distribuídos entre jogos ou equipes nem somados aos eventos.
- O portal antigo interpreta `ac=7` como relação/presença, com `val1=0` presente e `val1=1` reserva ausente na regra da La Liga. Não extrapolar essa regra para 2023, cuja amostra não tem `ac=7`.
- Sem súmula de presença válida, jogos disputados pelo jogador ficam desconhecidos. Não inferir presença apenas por estar no elenco ou ter agregado acumulado.
- Não inferir um gol individual para cada valor agregado. `MatchSourceEvent` preserva cada evento e `PlayerMatchStatistics` só recebe informação validada.
- A lista completa de fases/rodadas e todos os códigos de evento ainda precisam ser confirmados; as entidades já suportam essas informações sem adivinhar os valores.

## Fixtures e privacidade

[Fixtures sintéticas sanitizadas](../../backend/tests/Fixtures/CopaFacil/README.md) reproduzem as variações de estrutura observadas e o caso de placar nulo informado. Nenhuma resposta bruta foi adicionada ao Git. Respostas completas foram mantidas apenas em diretório temporário para a inspeção.

## Contrato de ingestão da próxima etapa

DTOs de origem ficam na Infrastructure, separados do domínio. Usar timeout, cancelamento, retentativas limitadas, hash de conteúdo e checkpoint. Preservar campos não reconhecidos. Recursos, referências ou eventos inválidos não devem sobrescrever o último estado válido. Cadastro de IDs de origem pertence a `EditionSource`; não publicar esses valores no README.

## Desserialização no Worker

O adaptador usa `System.Text.Json`, DTOs com `[JsonPropertyName]` em `Viw.Infrastructure/Imports/Dtos` e coleções `Dictionary<string, T>` para os registros indexados do Firebase. `CopaFacilJson` desserializa os recursos; `CopaFacilValidation` valida nomes, participantes e placares; o writer recebe os dados tipados.

Os dois conversores tratam apenas variações observadas: números em campos textuais e números inteiros representados com parte decimal zero. Strings numéricas em campos numéricos, frações em inteiros e tipos incompatíveis são rejeitados. Não há conversão permissiva que transforme erro em zero.

`JsonElement` permanece para agregados ainda não normalizados e cópia fiel dos snapshots, inclusive campos desconhecidos e eventos originais. O formato textual de estatísticas `chave=valor#...` é tratado separadamente por `SourceStatisticsParser`; não é JSON. A desserialização tipada não exige migration. A revisão `copafacil-v2` do mapeamento reprocessa os checkpoints para aplicar a regra confirmada de placar nulo em partidas encerradas.
