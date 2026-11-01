# Desenvolvimento local

## Situação atual

Existem documentação e diretórios de preparação. Ainda não existem `.csproj`, solução, migrações, aplicação Angular ou comando funcional de inicialização.

## Preparação prevista

- SDK .NET 10 e Git.
- SQL Server de desenvolvimento isolado, local em ambiente compatível ou remoto.
- Storage de desenvolvimento quando a importação de imagens começar.
- Node e ferramentas Angular apenas quando o Admin for implementado; a versão será fixada naquela etapa.

No macOS, não presumir instalação nativa do SQL Server em Apple Silicon. Nunca apontar testes para o banco de produção.

## Ao iniciar o Worker

1. Gerar solução e projetos em `backend/src`, com referências na direção documentada.
2. Fixar SDK em `global.json` e adicionar ignore adequado antes de gerar artefatos.
3. Definir opções tipadas e validação de configuração.
4. Adicionar conexão por User Secrets ou ambiente, com exemplo sem credenciais.
5. Criar modelo e migrações controladas.
6. Documentar comandos reais de restore, build, test e execução após verificá-los.

Configurar provedor, edições, timeouts, lotes e armazenamento. Não versionar senhas, tokens, banco local, dumps, snapshots pessoais ou logs. Fixtures de teste devem ser sanitizadas.

Os testes devem cobrir mapeamento, idempotência, correções, concorrência e recuperação; não depender da disponibilidade do Copa Fácil em toda execução.
