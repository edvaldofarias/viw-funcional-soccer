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

Os campos `dt.qt_p1` e `dt.qt_p2` são persistidos como placares de pênaltis opcionais, sem substituir `qt_g1`/`qt_g2`. O mapper v4 reimporta as partidas já processadas após aplicar a migração `AddMatchPenaltyScores`.

## Atualização de identidade — 02/11/2026

O Worker importa SourcePlayer sem criar Player. O Admin confirma identidades na Wiki e posições por copa; consulte [guia do Admin](../development/admin.md). As migrations de desenvolvimento foram consolidadas em InitialCompetition e InitialAdmin. Não aplicar essa baseline sobre banco antigo: esta alteração pressupõe recriação explícita do banco de desenvolvimento. Depois, aplicar ambas as migrations, criar o administrador e executar o Worker. A Wiki começa vazia até a confirmação das pessoas; os jogadores das copas aparecem normalmente.
