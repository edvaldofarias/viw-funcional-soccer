# Worker — primeira entrega

## Escopo

Importar uma edição encerrada e a La Liga, preservar origem, normalizar dados e persistir sem duplicação. A importação funciona antes da Web API e das interfaces.

## Pipeline

1. Selecionar edição e obter lease com expiração e proprietário.
2. Abrir ImportRun e consultar checkpoints.
3. Buscar recursos com concorrência limitada.
4. Preservar snapshot, hash, instante e versão do mapeamento.
5. Validar e preparar lotes coerentes.
6. Persistir fatos e mensagens Outbox em transação.
7. Atualizar projeções de forma idempotente.
8. Gravar progresso, contagens e estado final; liberar lease.

Lease expirado não permite que um executor antigo continue gravando sem verificar propriedade/revisão. Cancelamento deve encerrar o lote com segurança; não registrar sucesso se a edição ficou incompleta.

## Estados

Queued, Running, Succeeded, Failed e Cancelled. Importação sem alteração pode concluir com sucesso e contagem zero. Definir recuperação de Running abandonado com lease expirado; não deixar tarefa presa indefinidamente.

## Aceite

- Reexecução não duplica fatos.
- Correção de placar altera projeções com rastreabilidade.
- Timeout, resposta incompleta e indisponibilidade preservam o último estado válido.
- Interrupção permite retomada.
- Execuções simultâneas da mesma edição são impedidas.
- Dados administrativos oficiais permanecem intactos.
- Importação completa tem contagens e conferência contra a origem.

## Acionamento

O host Worker executa a lógica compartilhada. A API poderá receber solicitações autorizadas e persistir tarefas para o Worker. Uma alternativa para hospedagem compartilhada é executar lotes curtos pela API sob agendamento; ambos usam os mesmos locks e checkpoints. Não ativar duas estratégias sem coordenação.

Intervalos, políticas por edição e tamanho de lote serão configuráveis e validados com os limites da fonte e hospedagem. O Worker executa imediatamente e repete após 3600 segundos, configuráveis. Veja [execução e recuperação](running.md).

## Presença histórica e fases

O mapper `copafacil-v4` reprocessa checkpoints anteriores. Por convenção definida para o histórico até a Copa do Mundo 2026 (anos anteriores a 2026 e primeiro semestre de 2026), cada jogador do elenco ativo dos times de uma partida encerrada recebe uma presença, mesmo sem evento individual. Gols e cartões continuam derivados dos eventos efetivos. Snapshots e eventos originais não são alterados. Edições posteriores mantêm o tratamento de presença explícita.

`PhaseNames` permite associar IDs externos a nomes confirmados e à sua ordem. O catálogo contém o mapeamento conhecido da La Liga; fases históricas sem metadados mantêm seus nomes existentes.

`PhaseGroups` registra os grupos editoriais de uma fase (ex.: Grupo A/Grupo B da primeira fase). O contrato importado mapeia fase (`fs`), conjunto (`m_set`) e posição da equipe na fase (`dt`, campo `col`), mas não mapeia metadados oficiais dos grupos. A chave é o ID externo da fase e cada grupo lista os IDs externos das equipes, na ordem oficial de apresentação. A importação cria os grupos em `CompetitionGroup` e os vínculos em `CompetitionGroupTeam`, por edição/fase/equipe. Uma equipe pode participar de grupos em fases diferentes sem perder o vínculo anterior. Alterações do catálogo invalidam o checkpoint do core mesmo sem mudanças no JSON; grupos e vínculos removidos são reconciliados na importação seguinte. Exemplo:

```json
"PhaseGroups": {
  "1780479913323": [
    { "Name": "Grupo A", "Teams": ["id-externo-time-1", "id-externo-time-2"] },
    { "Name": "Grupo B", "Teams": ["id-externo-time-3", "id-externo-time-4"] }
  ]
}
```

Os IDs externos das equipes são as chaves do recurso `teams` da fonte, preservados em `TeamEntry.ExternalId` após a primeira importação — consulte o banco para montar a configuração de edições já importadas.

**Inferência automática:** somente a primeira fase, ordenada por `SortOrder`, recebe inferência quando não tem `PhaseGroups`. Quartas, semifinais, finais e disputas de colocação não geram grupos automaticamente: jogos isolados de mata-mata não representam grupos. Dois formatos são reconhecidos: cruzado (equipes com conjuntos idênticos de adversários, comparados por ID) e interno (componentes desconectados do grafo). Pontos corridos completos mantêm tabela única. A análise usa as partidas rastreadas do core, incluindo as ainda não salvas na primeira importação; nunca consulta apenas o estado anterior do banco. Nomes inferidos (Grupo A, Grupo B...) são ordenados pelos nomes das equipes, não comprovam as letras oficiais e podem ser ambíguos em calendários incompletos. Use `PhaseGroups` para fidelidade editorial; uma lista vazia desativa a inferência daquela fase.

O mapper `copafacil-v9` força o reprocessamento e remove os grupos artificiais de fases eliminatórias. A migration `ScopeGroupTeamsToPhase` transfere os vínculos antigos de `TeamEntry.GroupId` para `CompetitionGroupTeam` antes de remover a coluna. Aplicar a migration com API/Worker parados, atualizar as imagens e executar o Worker; não é necessário apagar banco, mídias ou contas.

Os campos `dt.qt_p1` e `dt.qt_p2` são persistidos como placares de pênaltis opcionais, sem substituir `qt_g1`/`qt_g2`. O mapper v4 reimporta as partidas já processadas após aplicar a migração `AddMatchPenaltyScores`.

## Atualização de identidade — 02/11/2026

O Worker importa SourcePlayer sem criar Player. O Admin confirma identidades na Wiki e posições por copa; consulte [guia do Admin](../development/admin.md). As migrations de desenvolvimento foram consolidadas em InitialCompetition e InitialAdmin. Não aplicar essa baseline sobre banco antigo: esta alteração pressupõe recriação explícita do banco de desenvolvimento. Depois, aplicar ambas as migrations, criar o administrador e executar o Worker. A Wiki começa vazia até a confirmação das pessoas; os jogadores das copas aparecem normalmente.
