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
