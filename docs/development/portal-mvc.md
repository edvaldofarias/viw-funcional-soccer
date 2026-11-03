# Portal MVC

O projeto `frontend/mvc/Viw.Mvc` consome apenas a API pública. Renderiza HTML no servidor, sem depender de JavaScript para navegação ou leitura dos dados. Não acessa diretamente o banco e não recebe a chave de monitoramento do Worker.

## Executar

Com banco criado pelas migrations, dados importados e Web API disponível:

```sh
dotnet run --project backend/src/Viw.WebApi
dotnet run --project frontend/mvc/Viw.Mvc
```

Configure `ViwApi__BaseUrl` com a origem da API e barra final. Configure `Site__PublicBaseUrl` com a origem pública definitiva, incluindo barra final. Em produção ela deve usar HTTPS. `Site__EnableIndexing` controla indexação; desenvolvimento desabilita por padrão. Não usar domínios de exemplo em produção.

Docker: `docker compose up --build webapi mvc`. O portal fica em `http://localhost:5081/`; configure conexão do banco antes. `VIW_PUBLIC_BASE_URL` ajusta a origem dos links no Compose. O ambiente do Compose é Development; configurar Production, HTTPS e indexação explicitamente na hospedagem definitiva.

## Rotas

| Página | Caminho |
| --- | --- |
| Catálogo | `/campeonatos` |
| Copa | `/campeonatos/{slug}` |
| Equipes e elenco | `/campeonatos/{slug}/equipes`, `/equipes/{id}` |
| Jogos e súmula | `/campeonatos/{slug}/jogos`, `/jogos/{id}` |
| Estatísticas | `/campeonatos/{slug}/estatisticas/jogadores` e `/estatisticas/equipes` |
| Prêmios oficiais | `/campeonatos/{slug}/premiacoes` |
| Memória das copas | `/wiki` e `/wiki/campeonatos/{slug}` |
| Busca e perfil | `/wiki/jogadores` e `/wiki/jogadores/{slug}` |
| Link estável por identidade | `/wiki/jogadores/id/{id}` redireciona 301 para o slug atual |

As rotas abreviadas da tabela continuam sob a copa correspondente. Slugs vêm do cadastro; nunca são deduzidos a partir do nome ou do identificador. Os links de jogador que partem de súmulas e prêmios usam a resolução por ID na API, preservando a navegação após alteração do slug.

## Conteúdo e qualidade

- Paginação e filtros de edição/ano, jogos e busca de jogador.
- Horários no fuso da edição; placar desconhecido aparece como travessão. Zero confirmado permanece zero.
- Estatísticas seguem os contratos da API: partidas encerradas, presença desconhecida e súmula incompleta são apresentados sem inventar participações ou regras de classificação.
- Premiações são registros oficiais; o portal não concede automaticamente o Ballon d’Or nem transforma o ranking de gols em prêmio confirmado.
- Técnicos, vínculos e nomes históricos são exibidos conforme o cadastro.
- Imagens usam monogramas enquanto não houver URL pública de mídia no contrato. IDs de assets não são convertidos em URLs privadas ou links com token do Firebase.
- Recursos ausentes retornam 404; filtros inválidos, 400; indisponibilidade ou resposta inválida da API, 503 com tentativa posterior. A API continua responsável pela preservação dos dados importados.

## Busca e compartilhamento

Título, descrição, URL canônica, Open Graph, Twitter e JSON-LD por página; imagem social local de 1200 × 630. A origem canônica vem da configuração e não do cabeçalho Host. Dados estruturados são serializados com escape seguro. Páginas filtradas e erros não são indexáveis.

`/robots.txt`, `/sitemap.xml` e três sitemaps (copas/equipes, jogadores, partidas) são gerados a partir da API paginada. Falha da API não produz um sitemap incompleto com sucesso. O limite atual é 50 mil URLs por arquivo; ampliar o particionamento antes de superar esse volume. Não há datas de atualização inventadas.

## Validação

```sh
dotnet test frontend/mvc/Viw.Frontend.slnx --configuration Release
```

Também podem rodar com `docker compose --profile tests run --build --rm mvc-tests`.

Os testes HTTP usam a aplicação MVC real com API simulada, AAA e Shouldly: HTML renderizado, metadados, navegação, erros, filtros, sitemaps e segurança dos nomes. Não exigem SQL Server. Os testes de integração do backend continuam necessários para validar as consultas reais.

Não publica, não aplica migrations e não altera prêmios ou identidades ao abrir uma página. Autenticação administrativa, acompanhamento restrito do Worker, entrega de mídia e publicação no SmarterASP.NET são trabalhos separados.

## Home institucional

A rota `/` apresenta a VIW e encaminha para campeonatos, jogadores, Wiki e o perfil oficial `https://www.instagram.com/viwfuncionalsoccer/`. É independente da API: uma indisponibilidade do catálogo não derruba a apresentação institucional. O catálogo completo permanece em `/campeonatos`.

A leitura automática do Instagram foi bloqueada durante a implementação. O texto é uma proposta editorial; fotos, identidade oficial e informações institucionais específicas aguardam material do perfil para validação. Não há feed automático nem imagens obtidas do Instagram.

A captura legível do Instagram enviada posteriormente orientou a revisão da Home: azul/preto, chamada “Sai do sofá, vem pra VIW”, fundamentos, circuitos, turmas/Kids e resenha. Horários e contatos não legíveis não foram inventados. A captura não é utilizada como foto nem como feed do site; fotos e logotipo oficiais em boa resolução podem substituir os elementos gráficos atuais.

## Visão do campeonato

O portal usa Bootstrap 5.3.8 com arquivos locais e licença MIT em `wwwroot/lib/bootstrap`. A primeira fase mostra posição e pontos informados pela fonte; valores ausentes aparecem como traço. Jogos são agrupados por fase, com links para as equipes e seus elencos. Os nomes confirmados das fases podem ser configurados em `PhaseNames` no catálogo do Worker, sem inferir regras de edições antigas.

Edições arquivadas oferecem `?visao=geral`: a classificação considera a fase mais avançada disputada, mantendo empates entre eliminados na mesma fase. Uma final empatada usa o placar de pênaltis quando informado; sem desempate conhecido, mantém a posição compartilhada. Melhor ataque e defesa usam totais de gols nos jogos encerrados, exibindo empates e quantidade de partidas. Artilharia vem das estatísticas individuais. Melhor jogador depende de Ballon d’Or confirmado no Admin; sem atribuição, uma mensagem explícita é exibida.

O Angular Admin usa o mesmo Bootstrap e é servido pela Web API em `http://localhost:5080/admin/`, sem contêiner Angular separado. Após atualizar o código, reconstrua os serviços de aplicação pelo Compose mantendo as variáveis de conexão e os volumes existentes.

## Wiki do jogador

O perfil canônico tem carreira por equipe e edição, tabela de estatísticas individuais e resultados nos jogos com presença conhecida, totais, disciplina, marcas e galeria com fotos originais de cada campeonato. As imagens e nomes históricos são preservados. Pontos 3/1/0 são uma métrica comparativa explicitada na página; não alteram a classificação oficial. A reatribuição no Admin reorganiza a Wiki sem mover fatos entre campeonatos.

As colunas CA/CV e o rodapé exibem cartões por campeonato e totais da carreira. O campo `Discipline` do histórico é da edição inteira, enquanto `Statistics` e `Results` permanecem restritos à equipe. Se houver transferências, os cartões da edição se repetem nas passagens, sem duplicação no total da carreira. Em edições arquivadas, cartões usam as chaves `14/16` dos agregados do Copa Fácil, quando disponíveis; sem cada chave, usam os eventos correspondentes das partidas encerradas. Em edições em andamento, usam somente súmulas de jogos encerrados. A mesma regra é usada nos totais do perfil e nas estatísticas de jogadores do campeonato. Não são fabricados eventos por partida e não é necessário reimportar ou migrar o banco para aplicar a correção aos agregados já preservados.

## Imagens próprias e alternativa da origem

O portal usa `/imagens/{kind}/{id}`, encaminhando para `/api/media/{kind}/{id}` na API. `kind`: players, teams ou editions; `editionId` escolhe foto histórica e `cover=true` a capa da edição. A API lê primeiro o arquivo identificado por hash no storage Local ou R2. Se faltar ou falhar (tempo limite de 2 segundos), redireciona para a URL original HTTPS cadastrada de um host permitido. Sem ambas, os avatares mantêm as iniciais. A API não recebe URL nem caminho arbitrário do cliente.

No Docker, API e Worker compartilham `worker-media`: o Worker grava em `/data/media` e a API monta o mesmo volume em `/media` somente para leitura e lê `/media/media`, separado das chaves do Admin. A origem Firebase permanece no banco. No R2, configure a mesma seção Media e credenciais nos dois serviços. A galeria usa a foto da edição, não substitui todas as fotos pela foto atual.
