# Roadmap

Fundação criada: solução `.slnx`, projetos .NET, hosts iniciais, Docker, testes básicos e baseline de migrations. A importação foi implementada; a aplicação das migrations e a carga real serão realizadas no Windows. Telas de negócio continuam pendentes.

## 1. Fundação e Worker

- [x] Gerar solução `.slnx`, projetos backend, MVC e infraestrutura Docker.
- [x] Preparar EF Core e ferramenta de migrations.
- [x] Documentar contrato observado de uma edição encerrada e da La Liga, com fixtures sanitizadas.
- [x] Modelar banco, snapshots, execuções, leases e checkpoints; gerar migration.
- [ ] Aplicar e testar no SQL Server real (CI configurada; local sem servidor ativo).
- [x] Implementar importação idempotente, consulta horária, fotos e testes de correção/retomada.
- [x] Configurar as oito edições; conferir recursos principais da origem.
- [x] Confirmar a representação de 0 × 0 em partidas encerradas com placar nulo e ajustar o mapeamento.
- [ ] Executar a carga real e conferir os dados persistidos.

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
