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
- Na tabela de estatísticas de jogadores, não exibir a coluna “Registros”. A coluna “Jogos” mostra somente o valor de `KnownAppearances`, inclusive zero, sem “confirmados” ou avisos de presença na célula. Os campos técnicos permanecem na API; o aviso geral de estatísticas é preservado.
- Em edições encerradas, todos os líderes empatados em gols recebem reconhecimento de artilheiro. Ballon d’Or e demais categorias continuam dependentes de confirmação oficial.
- Técnicos, vínculos e nomes históricos são exibidos conforme o cadastro.
- Imagens usam monogramas enquanto não houver URL pública de mídia no contrato. IDs de assets não são convertidos em URLs privadas ou links com token do Firebase.
- Recursos ausentes retornam 404; filtros inválidos, 400; indisponibilidade ou resposta inválida da API, 503 com tentativa posterior. A API continua responsável pela preservação dos dados importados.

### Tabelas ordenáveis (DataTables)

Todas as tabelas HTML do portal usam DataTables 3 com integração Bootstrap 5, sem jQuery ou CDN: classificações gerais e por grupo, elenco, súmula, estatísticas de jogadores/equipes e histórico da Wiki. Cabeçalhos permitem ordenar; Shift + clique combina colunas. Há busca local e páginas de 20 linhas inicialmente, com opções de 10, 20, 50 ou 100. A ordenação inicial da API é preservada, especialmente nas classificações; reordenar não recalcula posições oficiais.

`CollectTableAsync` reúne **todas** as páginas da API antes de renderizar os grids. Nunca inicializar a biblioteca sobre apenas uma página do servidor: isso ordenaria só uma parte do campeonato. As antigas URLs `?page=N` continuam válidas e escolhem a página inicial do DataTables; páginas fora do intervalo retornam 404. Os grids não exibem o paginador Razor. Sem JavaScript, todo o conjunto permanece legível no HTML; navegação por cartões continua paginada no servidor.

`site.js` prepara dados ortogonais: nomes sem monogramas/textos de acessibilidade, números com sinais e decimais em formato canônico e travessões como valores ausentes. Não alterar o texto exibido para conseguir ordená-lo. O modal do elenco continua associado aos botões, inclusive depois de ordenar ou mudar de página. Totais do rodapé da Wiki permanecem totais da carreira, não apenas da página visível.

As opções e os textos pt-BR ficam em `frontend/shared/table-options.json`, também consumido pelo Angular. O tema comum está em `frontend/shared/table-theme.css`: herda as cores do portal, alinha busca/seletor/paginador, usa setas compactas com nomes acessíveis em português e empilha os controles no celular. O paginador inferior fica oculto quando o resultado filtrado tem zero ou uma página, mantendo a contagem visível; reaparece automaticamente se busca ou tamanho de página voltarem a produzir mais páginas. O tema identifica esse estado pelos controles anterior e próxima simultaneamente desabilitados, conforme o markup acessível do DataTables.

A rolagem horizontal pertence somente à linha de dados (`.dt-layout-full`), nunca ao contêiner externo com os controles. Não reintroduzir os gutters e as margens padrão do Bootstrap no wrapper.

Os assets e a licença MIT são versionados em `wwwroot/lib/datatables`; o publish .NET não exige Node. Para atualizar a dependência ou regenerar as opções e o tema locais:

```sh
cd frontend/mvc
npm ci
npm run vendor
```

Versionar o lockfile e os arquivos regenerados. Depois de alterar opções compartilhadas, regenerar os assets MVC e recompilar o Angular. Falhas de inicialização são registradas no console e exibem aviso na tabela, sem ocultar os dados.

### Elenco da equipe

A tabela do elenco mostra gols, cartões amarelos (CA) e vermelhos (CV). Gols são os fatos das partidas encerradas da equipe e edição consultadas; cartões são da edição inteira, pela mesma regra da Wiki: agregados históricos disponíveis em edições arquivadas, senão súmulas encerradas. Em transferências, os cartões incluem todas as equipes da edição, conforme a observação exibida na página. Consultas são em lote, restritas aos cadastros da página, e não exigem identidade na Wiki.

Não exibir o estado técnico da inscrição como “Registro”, “Aberto” ou “Encerrado” em nenhuma interface. `ObservedFrom`/`ObservedUntil` continuam no contrato para compatibilidade e controle interno, não indicam inscrições públicas ou situação do campeonato. Estatísticas ausentes em uma resposta aparecem como travessão, não como zero inventado.

No contrato atual do elenco, `Player.Name` e `OriginalName` são ambos o nome importado da edição; por isso “Jogador” e “Nome na edição” podem ser idênticos. O slug canônico eventualmente recebido não deve ser usado nesta tela. O nome é um botão que abre detalhes do **cadastro da edição**, nunca um link para a Wiki.

O modal usa `<dialog>` nativo: foco contido e devolvido ao botão de origem, fechamento pelo botão, Escape ou fundo, rolagem interna e layout responsivo. O conteúdo é carregado sob demanda de `/campeonatos/{slug}/equipes/{id}/jogadores/{sourcePlayerId}` como parcial Razor não indexável, via API própria da edição. Falhas e 404 são informados no modal com tentativa posterior; fechar cancela a requisição pendente. Nenhum cadastro canônico é necessário.

O cabeçalho mantém foto/nome/camisa/posição da edição e a equipe do elenco consultado. Os números do modal são da **edição inteira**, inclusive outras equipes em transferências: gols, cartões, presenças confirmadas, vitórias, empates e derrotas. As limitações de presença e súmulas são sinalizadas. A artilharia é `1 + quantidade de cadastros com mais gols`, preserva empates e fica indisponível sem gols. Ao encerrar o campeonato, todos os líderes com gols recebem reconhecimento de artilheiro. Em edições arquivadas, a posição final da equipe reutiliza a classificação geral existente e mostra indisponibilidade quando desconhecida. Reconhecimentos incluem concessões editoriais confirmadas e artilharia automática do cadastro nesta edição, sem duplicação. A mesma regra alimenta a página de premiações e a Wiki vinculada; a interface identifica os reconhecimentos automáticos. Não reproduzir pesos, pontuação ou gráfico de MVP da referência visual: não há regra de MVP cadastrada.

## Busca e compartilhamento

Título, descrição, URL canônica, Open Graph, Twitter e JSON-LD por página; imagem social local de 1200 × 630. A origem canônica vem da configuração e não do cabeçalho Host. Dados estruturados são serializados com escape seguro. Páginas filtradas e erros não são indexáveis.

`/robots.txt`, `/sitemap.xml` e três sitemaps (copas/equipes, jogadores, partidas) são gerados a partir da API paginada. Falha da API não produz um sitemap incompleto com sucesso. O limite atual é 50 mil URLs por arquivo; ampliar o particionamento antes de superar esse volume. Não há datas de atualização inventadas.

## Validação

```sh
dotnet test frontend/mvc/Viw.Frontend.slnx --configuration Release
```

Também podem rodar com `docker compose --profile tests run --build --rm mvc-tests`.

Os testes HTTP usam a aplicação MVC real com API simulada, AAA e Shouldly: HTML renderizado, metadados, navegação, erros, filtros, sitemaps e segurança dos nomes. Não exigem SQL Server. Os testes de integração do backend continuam necessários para validar as consultas reais.

Listagens em grades de cartões (jogadores, campeonatos, equipes e jogos) usam 24 registros por página, completando linhas de duas ou três colunas. A prévia de jogos da equipe usa seis cartões. A última página pode ter uma linha incompleta quando restarem menos registros; nenhum item é duplicado ou omitido para preencher espaços. Listagens tabulares carregam o conjunto completo e usam paginação local do DataTables, inicialmente com 20 registros. Limites explícitos de prévias e consultas completas são preservados.

Não publica, não aplica migrations e não altera prêmios ou identidades ao abrir uma página. Autenticação administrativa, acompanhamento restrito do Worker, entrega de mídia e publicação no SmarterASP.NET são trabalhos separados.

## Home institucional

A rota `/` apresenta a VIW e encaminha para campeonatos, jogadores, Wiki e o perfil oficial `https://www.instagram.com/viwfuncionalsoccer/`. É independente da API: uma indisponibilidade do catálogo não derruba a apresentação institucional. O catálogo completo permanece em `/campeonatos`.

A leitura automática do Instagram foi bloqueada durante a implementação. O texto é uma proposta editorial; fotos, identidade oficial e informações institucionais específicas aguardam material do perfil para validação. Não há feed automático nem imagens obtidas do Instagram.

A captura legível do Instagram enviada posteriormente orientou a revisão da Home: azul/preto, chamada “Sai do sofá, vem pra VIW”, fundamentos, circuitos, turmas/Kids e resenha. Horários e contatos não legíveis não foram inventados. A captura não é utilizada como foto nem como feed do site; fotos e logotipo oficiais em boa resolução podem substituir os elementos gráficos atuais.

## Visão do campeonato

O portal usa Bootstrap 5.3.8 com arquivos locais e licença MIT em `wwwroot/lib/bootstrap`. A primeira fase mostra posição e pontos informados pela fonte; valores ausentes aparecem como traço. Jogos são agrupados por fase, com links para as equipes e seus elencos. Os nomes confirmados das fases podem ser configurados em `PhaseNames` no catálogo do Worker, sem inferir regras de edições antigas.

Quando a fase tem grupos — configurados em `PhaseGroups` no catálogo ou inferidos automaticamente dos confrontos na importação — a primeira fase exibe uma tabela por grupo (Grupo A, Grupo B...), fiel à apresentação da fonte, com a classificação geral da fase recolhida em um expansível. Dentro do grupo, as equipes são ordenadas pelos pontos oficiais e, no empate, pela posição global da fase registrada pela fonte; a posição exibida é relativa ao grupo. J/V/E/D e gols continuam considerando todos os jogos encerrados da fase, inclusive cruzados entre grupos. Fases sem grupos (pontos corridos, por exemplo) mantêm a tabela única.

A inferência automática se limita à primeira fase; confrontos do mata-mata não criam grupos. A API resolve os participantes por `CompetitionGroupTeam` (edição/fase/equipe), não por um grupo único no cadastro da equipe. Após a migration `ScopeGroupTeamsToPhase`, o mapper v9 reconcilia dados antigos sem apagar o banco. A validação deve conferir os nomes e a quantidade de linhas em cada `tbody`, não apenas a presença dos títulos Grupo A/B. Letras inferidas não substituem a configuração editorial oficial; consulte [o fluxo de importação](../worker/import-pipeline.md).

Edições arquivadas oferecem `?visao=geral`: a classificação considera a fase mais avançada disputada, mantendo empates entre eliminados na mesma fase. Uma final empatada usa o placar de pênaltis quando informado; sem desempate conhecido, mantém a posição compartilhada. Melhor ataque e defesa usam totais de gols nos jogos encerrados, exibindo empates e quantidade de partidas. Artilharia vem das estatísticas individuais. Melhor jogador depende de Ballon d’Or confirmado no Admin; sem atribuição, uma mensagem explícita é exibida.

O Angular Admin usa o mesmo Bootstrap e é servido pela Web API em `http://localhost:5080/admin/`, sem contêiner Angular separado. Após atualizar o código, reconstrua os serviços de aplicação pelo Compose mantendo as variáveis de conexão e os volumes existentes.

## Wiki do jogador

O perfil canônico tem carreira por equipe e edição, tabela de estatísticas individuais e resultados nos jogos com presença conhecida, totais, disciplina, marcas e galeria com fotos originais de cada campeonato. As imagens e nomes históricos são preservados. Pontos 3/1/0 são uma métrica comparativa explicitada na página; não alteram a classificação oficial. A reatribuição no Admin reorganiza a Wiki sem mover fatos entre campeonatos.

As colunas CA/CV e o rodapé exibem cartões por campeonato e totais da carreira. O campo `Discipline` do histórico é da edição inteira, enquanto `Statistics` e `Results` permanecem restritos à equipe. Se houver transferências, os cartões da edição se repetem nas passagens, sem duplicação no total da carreira. Em edições arquivadas, cartões usam as chaves `14/16` dos agregados do Copa Fácil, quando disponíveis; sem cada chave, usam os eventos correspondentes das partidas encerradas. Em edições em andamento, usam somente súmulas de jogos encerrados. A mesma regra é usada nos totais do perfil e nas estatísticas de jogadores do campeonato. Não são fabricados eventos por partida e não é necessário reimportar ou migrar o banco para aplicar a correção aos agregados já preservados.

Todos os perfis de jogadores mantêm os sete recordes visíveis: partidas com mais gols, maior vitória, maior derrota, melhor/pior classificação final e maiores sequências de vitórias/derrotas. `Records` no perfil da API considera toda a carreira vinculada, independentemente da página do histórico. Gols individuais usam fatos conhecidos; resultados e sequências usam presença registrada. Vitórias/derrotas são comparadas pela diferença de gols, sem pênaltis de desempate, preservando recordes empatados. Sequências continuam entre campeonatos, seguem as datas e são interrompidas por empate ou presença desconhecida; ausências confirmadas não contam como jogos do jogador. Datas ausentes ou coincidentes impedem confirmar a sequência. Classificações usam a visão geral das edições encerradas, sem inventar posições ausentes.

O quadro do jogador exibe jogos, gols, cartões amarelos/vermelhos e vitórias/empates/derrotas da carreira inteira. `Results` conta cada partida encerrada com participação confirmada uma vez, mesmo com cadastros duplicados; jogos sem placar conhecido não recebem resultado. `CupsPlayed` conta edições distintas com inscrição vinculada, incluindo campeonatos em andamento, sem duplicar transferências nem depender da página do histórico. Na interface, a contagem de presenças é rotulada como “Jogos” e participações registradas como “Copas disputadas”; os nomes dos campos existentes da API permanecem compatíveis.
O perfil tem sumário lateral no desktop, títulos de seção em estilo enciclopédico e identificação do clube na legenda da foto. `Podium` conta títulos, vice-campeonatos e terceiros lugares da carreira inteira, independentemente da paginação. Cada edição encerrada conta uma conquista, pela melhor posição entre as equipes vinculadas. O lateral usa dourado quando existe título, senão prata quando existe vice, senão bronze quando existe terceiro; sem pódio, mantém a aparência neutra. As conquistas seguem a classificação final e não dependem de premiações individuais. Não são inventados dados biográficos ausentes. `Results` conta cada partida encerrada com participação confirmada uma vez, mesmo com cadastros duplicados; jogos sem placar conhecido não recebem resultado. `CupsPlayed` conta edições distintas com inscrição vinculada, incluindo campeonatos em andamento, sem duplicar transferências nem depender da página do histórico. Na interface, a contagem de presenças é rotulada como “Jogos” e participações registradas como “Copas disputadas”; os nomes dos campos existentes da API permanecem compatíveis.

`Awards` reúne concessões individuais confirmadas e a artilharia automática das edições encerradas dos vínculos ativos, incluindo edição, rodada/partida e posição na seleção quando aplicáveis. A seção de prêmios e seu link no sumário aparecem somente quando existe ao menos um reconhecimento. Apenas categorias com reconhecimentos são exibidas, incluindo artilheiro, craque do campeonato (Ballon d’Or), seleções do campeonato/rodada, craque da partida e categorias cadastradas. Somente artilharia tem reconhecimento automático, para todos os líderes empatados com gols; demais categorias exigem confirmação. Os sete recordes continuam visíveis mesmo sem dados.

Carreira e galeria mostram os escudos das equipes pelos endpoints de mídia existentes. Cada participação da página tem um quadro na galeria, mesmo sem foto, com a classificação final do clube (`FinalPosition`) usando os mesmos critérios da visão geral e dos recordes. Campeonatos não encerrados e posições desconhecidas são indicados explicitamente. Fotos, escudos e avatares ausentes ou que falham ao carregar usam imagens cinzas do `https://placehold.co/`, também no Angular Admin. As URLs têm somente dimensões/cores e avisos genéricos fixos (“Sem foto” / “Sem foto disponível”) e as requisições usam `no-referrer`, sem enviar nomes ou endereços dos perfis ao serviço. Nenhum placeholder é armazenado como imagem no projeto; logotipo, compartilhamento e mídias importadas são preservados.

Títulos, vice-campeonatos e terceiros lugares ficam somente no quadro lateral, sem seção repetida no artigo ou no sumário. As seções amplas começam abaixo desse quadro. Carreira, recordes, prêmios e galeria reduzem o número de colunas conforme a largura realmente disponível, preservando cartões legíveis. A tabela mantém largura mínima de 900 pixels, a identificação da equipe/copa tem ao menos 240 pixels e as colunas numéricas não se comprimem. Em telas menores, a rolagem horizontal fica no quadro da tabela, acessível pelo teclado, incluindo a última coluna CV, sem transbordar a página.

A busca pública usa os mesmos critérios do Admin: acentos ignorados, partes e iniciais preservadas, sugestões de pequenas diferenças com Fastenshtein e resultados exatos primeiro. Filtros de edição e vínculos ativos são aplicados antes da comparação; nomes originais de vínculos encerrados não geram sugestões. Somente IDs/nomes/apelidos são lidos antes da classificação, e fotos são consultadas apenas para os jogadores da página.

## Imagens próprias e alternativa da origem

O portal usa `/imagens/{kind}/{id}`, encaminhando para `/api/media/{kind}/{id}` na API. `kind`: players, teams ou editions; `editionId` escolhe foto histórica e `cover=true` a capa da edição. A API lê primeiro o arquivo identificado por hash no storage Local ou R2. Se faltar ou falhar (tempo limite de 2 segundos), redireciona para a URL original HTTPS cadastrada de um host permitido. Sem ambas, o cliente exibe a imagem cinza do placehold.co. A API não recebe URL nem caminho arbitrário do cliente.

No Docker, API e Worker compartilham `worker-media`: o Worker grava em `/data/media` e a API monta o mesmo volume em `/media` somente para leitura e lê `/media/media`, separado das chaves do Admin. A origem Firebase permanece no banco. No R2, configure a mesma seção Media e credenciais nos dois serviços. A galeria usa a foto da edição, não substitui todas as fotos pela foto atual.
