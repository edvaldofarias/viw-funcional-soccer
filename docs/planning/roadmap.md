# Roadmap

Todas as entregas de software abaixo estão pendentes. Documentação inicial e diretórios de preparação foram criados.

## 1. Fundação e Worker

- Gerar solução e projetos backend.
- Descobrir contratos de uma edição encerrada e da La Liga.
- Modelar banco, snapshots, execuções, leases e checkpoints.
- Implementar importação idempotente e testes de correção/retomada.
- Conferir dados e importar demais edições.

## 2. Web API

- Publicar consultas e contratos OpenAPI.
- Expor progresso e tarefas de importação com autorização.
- Definir autenticação e comandos administrativos auditados.

## 3. MVC

- Portal das copas, partidas, estatísticas e Wiki.
- URLs permanentes e metadados de busca/compartilhamento.
- Acompanhamento autorizado do Worker via API.

## 4. Angular Admin

- Identidades e vínculos reversíveis.
- Artilheiro oficial, Ballon d’Or, craques, técnicos e seleções.
- Fotos oficiais e revisão de divergências da importação.

## 5. Operação

- Validar hospedagem e estratégia do executor.
- Automatizar testes, publicação e migrações controladas.
- Testar backup/restauração e preparar migração do portal existente.

## Decisões pendentes

Execução definitiva do Worker na hospedagem, autenticação entre hosts, roteamento IIS, versões do Angular, limites de ingestão e política de retenção dos snapshots. Resolver cada item antes de implementar a funcionalidade correspondente.
