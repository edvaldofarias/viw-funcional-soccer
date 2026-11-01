# Aplicações, rotas e acesso

## MVC público

Renderiza o HTML no servidor consumindo a Web API. Rotas planejadas:

```text
/campeonatos
/campeonatos/{edicao}
/campeonatos/{edicao}/classificacao
/campeonatos/{edicao}/jogos
/campeonatos/{edicao}/artilharia
/campeonatos/{edicao}/premiacoes
/wiki/jogadores/{slug-id}
/wiki/campeonatos/{edicao}
```

Manter URLs permanentes, canonical, sitemap, títulos e metadados de compartilhamento. Mudanças de nomes preservam os links por redirecionamento 301. Falha da API deve apresentar indisponibilidade adequada, não uma página vazia com sucesso HTTP.

## Acompanhamento do Worker

O MVC poderá mostrar situação, último sucesso e andamento em `/operacao/importacoes`, com autorização. Essa tela consulta a API e não inicia um processo local. Solicitações de execução criam tarefas persistidas, quando autorizadas. Logs detalhados e snapshots não são públicos.

## Angular Admin

Gerencia identidades, técnicos, seleções e prêmios pela API. O Angular é servido em `/admin/` pelo host da API, junto de `/api/admin`. O MVC público é outro host. Consulte [configuração do Admin](../development/admin.md).

## Web API

Consultas HTTP implementadas sob `/api/v1`, documentadas por OpenAPI no ambiente Development. Veja [rotas e execução](../development/web-api.md). Separar consultas públicas, comandos administrativos e endpoints internos do agendador. Comandos longos retornam referência de tarefa, com autorização para consultar seu progresso. Nenhum endpoint de saúde público revela segredos.

## Autenticação

Identidade e permissões são verificadas no servidor; esconder botões no Angular não é autorização. Priorizar publicação sob a mesma origem para simplificar cookies seguros e política de acesso. Cookies autenticados exigem proteção CSRF, inclusive no Angular. MVC encaminha a identidade de forma explícita e confiável, sem um token administrativo global compartilhado entre usuários.

O monitoramento usa atualmente uma chave exclusiva de leitura servidor-a-servidor, sem acesso administrativo. Essa chave não será distribuída aos usuários. O Angular usa sessão ASP.NET Core Identity na origem da API; o MVC público não recebe credenciais administrativas. Se houver origens distintas, definir CORS restrito e fluxo de autenticação apropriado; não liberar origem curinga com credenciais. Adotar ASP.NET Core Identity para os cadastros administrativos e MFA conforme a implementação do acesso.
