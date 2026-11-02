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

**Unir jogadores:** comparar um cadastro original de uma copa com a identidade canônica de destino. Confirmar nome atual, destino e motivo. O vínculo anterior é encerrado; um novo é criado em transação serializável, sem apagar fatos, fotos ou registros da fonte. Reversão usa o mesmo fluxo para reatribuir à identidade anterior. Não há união automática por semelhança, exclusão de jogador ou fusão em lote. Prêmios e decisões manuais não são transferidos automaticamente: revisar seus destinatários após uma reatribuição.

**Prêmios:** artilheiro oficial, Ballon d’Or, craque da partida, melhor técnico e seleções por posição da rodada/campeonato. Cada concessão pode ser rascunho, confirmada ou revogada. Apenas confirmadas aparecem na API pública. A migração adiciona categorias sem conceder nenhum prêmio e preserva categorias já cadastradas. É permitido mais de um artilheiro quando a decisão oficial assim determinar. Partidas e rodadas precisam ser da edição; jogador precisa ter vínculo ativo nessa copa, técnico precisa ter designação. Seleções exigem posição.

**Técnicos:** cadastro oficial e designação à equipe por período, opcionalmente por rodada. Corrigir ou encerrar com justificativa. O formulário usa horário do dispositivo e envia offset UTC; isso não altera o fuso do campeonato.

**Auditoria:** responsável autenticado, data, motivo e antes/depois. Alteração esportiva e auditoria são gravadas na mesma unidade transacional. Registros de gestão de contas ficam nos eventos de segurança, sem senhas, tokens ou hashes. O histórico é somente leitura no painel.

Concorrência usa RowVersion em cadastros/prêmios/designações e o vínculo ativo observado na identidade. Alterações desatualizadas retornam 409 em vez de sobrescrever outra decisão. As alterações manuais não modificam snapshots ou o método estatístico.

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

Em **Unir jogadores**, escolha um cadastro da origem, use **Revisar vínculo**, selecione uma identidade de destino e informe o motivo. Repita para os demais cadastros usando sempre o mesmo destino. O cadastro que já pertence ao destino não precisa ser reatribuído. As edições e seus números permanecem separados; a Wiki consulta a identidade comum e soma a carreira. Os vínculos anteriores e o motivo ficam auditados, permitindo desfazer a atribuição. Identidades sem vínculo ativo deixam de aparecer na busca pública, mas permanecem no Admin e no histórico.

Na criação de prêmios para jogadores, a busca envia `editionId` para `GET /api/admin/players`: somente identidades com vínculo ativo a um cadastro da edição selecionada são elegíveis. Trocar a edição limpa a escolha e descarta respostas antigas. A busca em Unir jogadores permanece global. O salvamento também valida o vínculo no servidor.

## Jogador principal e cadastros por copa

`Player` é a pessoa principal. `SourcePlayer` preserva cada cadastro recebido de uma copa. `PlayerIdentityLink` associa esses cadastros à pessoa; vínculos encerrados permanecem auditados. A tela mostra os campeonatos já vinculados a cada destino para distinguir homônimos. Escolha um principal, depois reatribua cada cadastro das outras copas sempre ao mesmo destino. Não reatribua o cadastro que já pertence ao principal. `PlayerMatchStatistics` e as fotos originais continuam ligados ao cadastro/edição; a Wiki consolida pela identidade ativa.
