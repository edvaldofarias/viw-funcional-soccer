# Integração Copa Fácil

Base observada na integração existente:

```text
https://copafacil-web.firebaseio.com/events/{externalChampionshipId}/
```

Verificar por edição os recursos `info.json`, `matchs.json`, `teams.json` e `player.json`. `matchs.json` é a grafia observada; não assumir `matches.json`. Os identificadores reais não são publicados nesta documentação.

## Descoberta antes de implementar

- Confirmar disponibilidade, estrutura, tipos, datas e estados das partidas.
- Identificar chaves de equipe, jogador e partida e suas referências.
- Distinguir placar válido, jogo sem resultado e estatística ausente.
- Identificar fotos, URLs temporárias e possíveis diferenças entre anos.
- Criar fixtures sanitizadas e uma tabela de mapeamento por versão do contrato.

O adaptador usa DTOs da origem separados do domínio. Validar resposta HTTP e conteúdo, timeout, cancelamento e retentativas limitadas. Resposta nula não autoriza apagar o acervo. Não presumir que IDs sejam globais entre edições.

## Configuração e dados

A base URL é configuração do provedor. O vínculo entre edição interna e ID externo é dado de integração persistido conforme o modelo de origem; não há mudança para remover esses vínculos do banco. Segredos ficam fora do Git. O cadastro inicial e seu mecanismo de provisionamento serão definidos na implementação, sem IDs reais em exemplos do README.
