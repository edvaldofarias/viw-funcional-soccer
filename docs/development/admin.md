# Administração da VIW

## Estrutura e execução

`frontend/viw-admin` contém somente o Angular, inclusive dependências e testes. `frontend/mvc` contém todo o .NET do frontend: solução, SDK, propriedades, portal MVC e testes. A API administrativa continua no backend.

O Admin é servido pela Web API em `/admin/`, na mesma origem de `/api/admin`. Pode usar um domínio como `admin.viwfuncionalsoccer.com.br` apontando para a API; o portal MVC público pode continuar em outro host. Não habilitar CORS com credenciais para origens genéricas. Em desenvolvimento, o proxy Angular encaminha `/api` para a API local.

## Banco e primeiro acesso

Não há migrations automáticas no startup e não há cadastro público. Faça backup e aplique os dois conjuntos de migrations de maneira controlada. `ViwDbContext` contém os dados esportivos e as categorias oficiais; `AdminIdentityDbContext` contém contas, perfis e eventos no schema `admin`, com histórico próprio `admin.__AdminMigrationsHistory`.

Com SDK .NET 10 e EF CLI 10, configure `ConnectionStrings__ViwDatabase` no ambiente ou User Secrets, sem versionar a senha:

```sh
cd backend
dotnet tool restore
dotnet ef database update --project src/Viw.Infrastructure --context ViwDbContext
dotnet ef database update --project src/Viw.WebApi --context AdminIdentityDbContext
cd ..
```

O manifesto da ferramenta está em `backend/.config/dotnet-tools.json`. Use `dotnet tool restore --tool-manifest backend/.config/dotnet-tools.json` para restaurar; execute a partir de `backend` para usar EF 10 do manifesto, em vez da ferramenta global antiga. A fábrica de design das migrations usa a variável de ambiente da conexão.

Para criar **somente a primeira conta**, configure temporariamente `Admin__BootstrapEmail` e `Admin__BootstrapPassword` via ambiente ou `Admin:BootstrapEmail`/`Admin:BootstrapPassword` via User Secrets da API. A senha exige 12 caracteres, maiúscula, minúscula, número e símbolo. Não há valores padrão.

```sh
dotnet run --project backend/src/Viw.WebApi -- --bootstrap-admin
```

O comando encerra após criar a conta e recusa executar se qualquer conta já existir. Não reseta senhas nem promove contas existentes. Remova as configurações de bootstrap após usá-lo. O Admin cria os acessos seguintes; cada usuário pode trocar a própria senha. Recuperação por e-mail, MFA e edição de perfis de contas existentes ainda não foram implementadas; não são simuladas pela interface.

## Permissões e sessão

- **Administrator:** cadastros esportivos, auditoria, criação de contas e bloqueio/liberação de acessos.
- **Editor:** jogadores, vínculos de identidade, técnicos, designações, prêmios, seleções e leitura da auditoria esportiva. Não gerencia contas.
- Cookie de sessão HttpOnly, SameSite Strict, HTTPS obrigatório em produção e duração máxima de oito horas. Senhas são hashes do ASP.NET Core Identity, sem armazenamento no Angular.
- Login limitado por endereço remoto e bloqueio temporário da conta após cinco falhas. A chave do monitoramento do Worker **não** autentica o Admin.
- Mutações exigem token CSRF no cabeçalho `X-VIW-CSRF`. O cliente obtém `/api/admin/session/csrf` antes do login e novamente após mudanças da sessão.
- Bloqueio de conta invalida sessões existentes; não é permitido bloquear a própria conta. O perfil existente não pode ser trocado nesta primeira interface: criar o novo acesso adequado e bloquear o anterior preserva a trilha.
- Persistir as chaves de Data Protection em `Admin__DataProtectionKeysPath`, com acesso restrito e backup. O Compose usa volume próprio, acessível ao usuário do contêiner. Para múltiplas instâncias, compartilhar as mesmas chaves. Não usar a chave de monitoramento como senha administrativa.

## Gestão esportiva

**Jogadores:** pesquisa, nome e slug oficiais. Os nomes originais ficam na fonte. Slugs precisam ser únicos. O contrato também aceita foto oficial como referência a asset público já cadastrado; a tela preserva a imagem atual. Upload de novas fotos e entrega pública do storage continuam na etapa de mídia.

**Unir jogadores:** escolha um cadastro, filtrando por copa. O assistente percorre as demais copas: escolha um jogador ou nenhum em cada etapa. Nomes diferentes podem ser pesquisados. Revise os cadastros e escolha o nome final; não é necessário motivo. Um único cadastro também pode criar a pessoa. Para editar, abra qualquer cadastro já vinculado; adicione ou remova cadastros e salve. Excluir a pessoa libera os cadastros e remove sua Wiki, preservando fotos, estatísticas e prêmios das copas. A auditoria registra automaticamente autor, instante e estado anterior.

**Posições e goleiros:** filtre por copa e equipe, escolha Goleiro, Zagueiro, Meio-campo, Ala ou Atacante e salve. Sem posição definida, o cadastro é tratado como Jogador de linha. O filtro Só goleiros mostra os goleiros confirmados. A posição vale apenas para a copa selecionada e não é sobrescrita pelo Worker.

**Prêmios:** artilheiro oficial, Ballon d’Or, craque da partida, melhor técnico e seleções por posição da rodada/campeonato. Cada concessão editorial pode ser rascunho, confirmada ou revogada; somente as confirmadas aparecem na API pública. Além desses registros, o portal reconhece automaticamente todos os líderes empatados com gols em uma edição encerrada, sem criar concessões no Admin. Sem gols registrados, não há artilharia automática. A migração adiciona categorias sem conceder nenhum prêmio e preserva categorias já cadastradas. Partidas e rodadas precisam ser da edição; jogador precisa ser um SourcePlayer dessa copa, técnico precisa ter designação. Seleções exigem posição.

**Técnicos:** cadastro oficial e designação à equipe por período, opcionalmente por rodada. Corrigir ou encerrar com justificativa. O formulário usa horário do dispositivo e envia offset UTC; isso não altera o fuso do campeonato.

**Auditoria:** responsável autenticado, data, motivo e antes/depois. Alteração esportiva e auditoria são gravadas na mesma unidade transacional. Registros de gestão de contas ficam nos eventos de segurança, sem senhas, tokens ou hashes. O histórico é somente leitura no painel.

Concorrência usa RowVersion em cadastros/prêmios/designações e identidades. Alterações desatualizadas retornam 409 em vez de sobrescrever outra decisão. As alterações manuais não modificam snapshots ou o método estatístico.

## Tabelas ordenáveis

Todos os grids HTML usam DataTables 3 / Bootstrap 5, sem jQuery ou serviços externos. As listagens de jogadores, técnicos, cadastros para unir, posições, prêmios editoriais, designações e contas usam `Api.all` para obter **todas as páginas do conjunto filtrado** antes da ordenação local. Preservar os filtros de nome, copa, equipe, vínculo e goleiros. Não substituir por uma única chamada paginada, pois isso limitaria a ordenação aos registros daquela página. Busca, tamanho de página e ordenação são locais; ações editoriais continuam na API, com CSRF e RowVersion.

`ViwDataTable` centraliza o ciclo de vida: destrói a instância e restaura os nós originais do `tbody`, **inclusive as âncoras de comentário do `@for`**, antes de o Angular reconciliar outra coleção. Inicializa após renderizar e destrói ao sair da tela. Restaurar apenas as linhas não basta: a transição vazio → registros perde a âncora do Angular. Alterações dos selects invalidam o cache de ordenação/busca; `data-order`/`data-search` usam o nome sem monograma e a posição selecionada, nunca todas as opções do select. Datas de designação ordenam por instante absoluto.

Os testes reais da diretiva cobrem mais de uma página, números 2/10, busca sem acentos, botões após ordenar/paginar, atualização de linhas rastreadas, selects, vazio → registros e destruição. Ordens, busca e tamanho de página são preservados nas recargas da mesma tabela; a página é limitada ao novo tamanho. Auditoria em cartões e candidatos do assistente **não** são tabelas e mantêm paginação no servidor.

As configurações pt-BR e os tamanhos de página são compartilhados com o MVC em `frontend/shared/table-options.json`. O CSS comum `frontend/shared/table-theme.css` é carregado depois dos estilos do Admin, com as cores administrativas, paginação compacta acessível e controles responsivos fora da rolagem horizontal dos dados. O paginador inferior só aparece com mais de uma página do resultado filtrado; a contagem permanece visível, inclusive após buscas, mudanças no tamanho de página ou recargas. Alterações nesse tema também exigem `npm run vendor` em `frontend/mvc`. As rotas Angular carregam componentes sob demanda para manter o bundle inicial dentro do orçamento existente. O Docker copia também a pasta compartilhada antes do build.

## Publicação e Docker

`docker compose up --build webapi` compila o Angular e o inclui no pacote da API; acessar `http://localhost:5080/admin/` em desenvolvimento. Para build manual:

```sh
cd frontend/viw-admin
npm ci
npm test
npm run build
```

Copiar o conteúdo de `dist/viw-admin/browser` para `backend/src/Viw.WebApi/wwwroot/admin` antes de publicar a API. Esses arquivos gerados são ignorados no Git. Não copiá-los para o portal MVC. Em IIS, encaminhar `/admin` e `/api` para o mesmo host ASP.NET Core e manter o fallback `/admin/{rota}`.

Em Docker, aplicar separadamente `docker compose --profile migrations run --build --rm migrations` e `docker compose --profile migrations run --build --rm admin-migrations` após configurar a conexão.

Antes de produção: aplicar migrations, configurar HTTPS, chaves persistentes e conexão restrita; criar o primeiro administrador e remover bootstrap. A publicação não foi executada por esta implementação.

## Testes

- Angular: `npm test` em `frontend/viw-admin`; sessão, CSRF, expiração e erros.
- Backend: testes HTTP de autorização/CSRF; fluxo real de login, troca de senha e logout com Identity sobre SQLite isolado; testes SQL das migrations, vínculos reversíveis, RowVersion, auditoria e concessões.
- MVC: `dotnet test frontend/mvc/Viw.Frontend.slnx`.
- CI executa SQL Server isolado. Localmente testes SQL ficam ignorados se `ConnectionStrings__ViwTestServer` não estiver configurada. Nenhum teste deve usar o banco de produção.

## Uma pessoa em vários campeonatos

O Worker cria somente SourcePlayer. Copas, inscrições, estatísticas e prêmios consultam esse cadastro mesmo sem Wiki. Player nasce apenas na confirmação do assistente. A Wiki reúne os cadastros vinculados, mostra seus nomes originais e permite busca por qualquer nome ou apelido importado.

Cada SourcePlayer pertence a no máximo um Player ativo. Cada Player aceita no máximo um SourcePlayer por edição. Dois homônimos na mesma copa são pessoas diferentes. O banco reforça essas regras com índices únicos filtrados e chave estrangeira composta por edição e cadastro.

A busca de destinatários de prêmios usa GET /api/admin/sources?editionId=...; não exige Player. Identidades usam POST/PUT/DELETE /api/admin/identities, com versão para alterações e exclusões. Posições usam PUT /api/admin/sources/{id}/position. Os contratos estão documentados no OpenAPI.

### Filtros da união de jogadores
A busca inicial permite filtrar por copa, equipe, nome opcional e somente cadastros ainda sem Player ativo. Em cada etapa da união, o filtro de equipe corresponde à copa atual e é limpo ao mudar de copa. Para buscar pelo time sem lembrar o nome, deixe a busca por nome vazia. Os filtros são aplicados antes da paginação.

A busca por nome considera qualquer parte significativa do nome composto, apelidos e iniciais: “Edvaldo Farias” também encontra “Edvaldo”, “Farias” e “E. Santana”. Os mesmos critérios são usados na busca dos jogadores principais e na Wiki; vínculos encerrados não fornecem apelidos para a identidade. Resultados não são confirmações nem disparam uniões automáticas.

As buscas ignoram acentos, diferenças entre maiúsculas/minúsculas e separadores. Fastenshtein compara palavras para sugerir pequenas diferenças de escrita: até uma edição para termos de 3–5 letras e duas para termos de 6 ou mais letras. Termos menores e números não recebem comparação aproximada. Nomes completos exatos vêm primeiro, seguidos de partes exatas, iniciais e aproximações. Os filtros de copa/equipe, goleiros e vínculos são aplicados no SQL antes de ler somente IDs e nomes/apelidos. A classificação é feita antes da paginação, sem truncar candidatos nem duplicar identidades; apenas os registros da página recebem os demais detalhes. Comparadores e resultados de nomes repetidos são reutilizados dentro da requisição, nunca entre requisições, evitando buffers concorrentes ou resultados antigos após edição/importação. Sem texto, permanece a consulta paginada diretamente no SQL. Não há migração ou reimportação.

Os candidatos mostram retratos de 120 × 150 pixels sem recortar a foto e todas as equipes registradas para o cadastro naquela copa. `SourceRow.Teams` preserva transferências e remove repetições da mesma equipe; ausência de inscrição é indicada como “Equipe não registrada”. O retrato maior também aparece na revisão da seleção. Fotos indisponíveis usam o aviso genérico “Sem foto disponível”, sem enviar nomes ao serviço externo.

O topo do assistente mostra o jogador base de comparação com o mesmo retrato ampliado, nome, copa e equipes. A referência é o cadastro clicado ao iniciar ou editar a pessoa, não o primeiro vínculo retornado nem o candidato selecionado na etapa atual. Ela permanece ao mudar copas, buscar/paginar candidatos e revisar a seleção, sem bloquear a remoção de vínculos. Ao fechar o assistente, a referência é limpa; uma nova união usa sua própria base.
