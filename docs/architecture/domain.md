# Domínio, identidade e informações oficiais

## Edições e fatos

Separar Competition, ChampionshipEdition, RuleSet e TeamEntry. Regras de pontuação, fases e desempates pertencem à edição. Registrar Match e PlayerMatchStatistics com origem e revisão; não converter estatística indisponível em zero.

## Identidade

Player representa somente a pessoa confirmada manualmente pelo Admin; o Worker nunca cria Player; SourcePlayer preserva o cadastro externo; PlayerRegistration registra equipe, edição e período. A chave externa combina provedor, edição e ID original.

Nomes semelhantes geram sugestões, nunca união automática. Um vínculo manual é auditado e reversível. Recalcular carreira sem apagar fatos nem duplicar participações. Equipes de edições diferentes também não são unificadas apenas pelo nome.

## Administração

Persistir artilheiro oficial (inclusive empate), Ballon d’Or da edição, craque da partida, seleções e técnicos por rodada/período. Usar AwardDefinition e AwardGrant com situação rascunho/confirmado, observação e auditoria. Distinguir liderança calculada em gols de prêmio oficial confirmado.

A importação não sobrescreve decisões manuais. Mudanças na fonte podem sinalizar divergências para revisão. Registrar autor, instante, motivo e valores anteriores; alterações de técnicos não reescrevem o passado.

## Identidade confirmada e posição por edição

Uma identidade pode ter um único cadastro. SourcePlayer é independente e permanece nas copas mesmo sem Player. PlayerIdentityLink aceita um cadastro ativo por edição por pessoa e apenas uma pessoa ativa por cadastro. Remover uma identidade preserva a origem e as premiações (AwardGrant.SourcePlayerId). A busca da Wiki usa nome final, nomes originais e apelidos de todos os vínculos ativos.

SourcePlayer.AssignedPosition é a posição manual da copa: Goleiro, Zagueiro, Meio-campo, Ala ou Atacante; null significa Jogador de linha. SourcePlayer.Position preserva o dado importado, sem substituir a decisão manual.
