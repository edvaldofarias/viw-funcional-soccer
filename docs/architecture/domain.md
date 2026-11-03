# Domínio, identidade e informações oficiais

## Edições e fatos

Separar Competition, ChampionshipEdition, RuleSet e TeamEntry. Regras de pontuação, fases e desempates pertencem à edição. Registrar Match e PlayerMatchStatistics com origem e revisão; não converter estatística indisponível em zero.

## Identidade

Player representa somente a pessoa confirmada manualmente pelo Admin; o Worker nunca cria Player; SourcePlayer preserva o cadastro externo; PlayerRegistration registra equipe, edição e período. A chave externa combina provedor, edição e ID original.

Nomes semelhantes geram sugestões, nunca união automática. Um vínculo manual é auditado e reversível. Recalcular carreira sem apagar fatos nem duplicar participações. Equipes de edições diferentes também não são unificadas apenas pelo nome.

## Administração

Persistir decisões editoriais, como Ballon d’Or da edição, craque da partida, seleções e técnicos por rodada/período, em AwardDefinition/AwardGrant com situação rascunho/confirmado, observação e auditoria.

**Artilharia de edição encerrada:** todos os cadastros empatados no maior total positivo de gols recebem o reconhecimento de artilheiro. Somar fatos das partidas encerradas da edição inteira, inclusive transferências; não usar carreira, gols de partidas em andamento ou desempate por nome. Sem gols registrados não há artilheiro. Antes de encerrar a edição, liderança não gera reconhecimento automático.

A leitura pública compartilha essa regra entre premiações, modal da edição e carreira vinculada. O reconhecimento automático é derivado dos fatos, não grava AwardGrant em GET, tem ID estável e `Automatic=true`, com `ConfirmedAt=null` (não inventar data de confirmação). Uma concessão editorial confirmada para um líder é reutilizada sem duplicação; concessões antigas de artilharia de quem não lidera deixam de compor a leitura pública encerrada, mas não são apagadas nem sobrescritas no Admin. Alterar gols ou reabrir a edição recalcula a parte automática. Demais categorias continuam exigindo confirmação editorial.

A importação não sobrescreve decisões manuais. Mudanças na fonte podem sinalizar divergências para revisão. Registrar autor, instante, motivo e valores anteriores; alterações de técnicos não reescrevem o passado.

## Identidade confirmada e posição por edição

Uma identidade pode ter um único cadastro. SourcePlayer é independente e permanece nas copas mesmo sem Player. PlayerIdentityLink aceita um cadastro ativo por edição por pessoa e apenas uma pessoa ativa por cadastro. Remover uma identidade preserva a origem e as premiações (AwardGrant.SourcePlayerId). A busca da Wiki usa nome final, nomes originais e apelidos de todos os vínculos ativos.

SourcePlayer.AssignedPosition é a posição manual da copa: Goleiro, Zagueiro, Meio-campo, Ala ou Atacante; null significa Jogador de linha. SourcePlayer.Position preserva o dado importado, sem substituir a decisão manual.
