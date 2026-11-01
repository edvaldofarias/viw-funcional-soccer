# Arquitetura

## Componentes

`backend/src` reúne Domain, Application, Infrastructure, Contracts, Worker e WebApi. `frontend` reúne MVC e o futuro Angular Admin.

- Domain não depende de EF Core, HTTP ou interfaces de apresentação.
- Application depende de Domain e define casos de uso e interfaces para serviços externos.
- Infrastructure implementa persistência, provedor Copa Fácil e storage.
- Worker e WebApi compõem essas dependências; não concentram regras em executores ou controllers.
- MVC consome contratos HTTP da API. Angular usa clientes tipados derivados do OpenAPI quando disponível.
- Contracts contém modelos de transporte e não referencia entidades de persistência. Não criar um único DTO abrangente para todos os contextos.

## Dados e processamento

SQL Server é o banco inicial; EF Core gerencia o mapeamento e as migrações. Worker escreve fatos e projeções; Web API consulta dados e executa comandos administrativos. MVC e Angular não têm credenciais do banco.

CQRS é uma separação lógica no mesmo banco. Outbox e consumidores idempotentes permitem atualizar projeções. Snapshots e auditoria preservam a origem e decisões, sem adotar Event Sourcing completo inicialmente.

Web API e Worker compartilham acesso ao banco por projetos internos, sem se tornarem microsserviços com bancos independentes. Migrações têm um único responsável por publicação; não rodar automaticamente em vários hosts concorrentes.

## Limites

O estado operacional deve ficar no banco: tarefas, checkpoints, leases, revisões e falhas. Arquivos temporários e memória do processo não são a fonte de verdade. Fotos ficam em storage; metadados no banco. Separar snapshots privados de mídia pública.
