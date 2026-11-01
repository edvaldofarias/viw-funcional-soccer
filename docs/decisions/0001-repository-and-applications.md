# ADR 0001 — Organização e aplicações

Status: aceito como direção arquitetural; fundação implementada, funcionalidades de negócio pendentes.

## Contexto

O portal precisa de páginas públicas permanentes e renderizadas no servidor, ingestão independente e administração futura em Angular. O primeiro desenvolvimento será o Worker.

## Decisão

Usar `backend`, `frontend` e `docs` na raiz. Backend contém Worker e Web API .NET com bibliotecas compartilhadas. Frontend contém MVC e Angular Admin futuro. MVC consome a API e exibe, mediante autorização, o acompanhamento das importações. Worker continua no backend.

Usar .NET 10, EF Core e SQL Server como direção inicial. CQRS no mesmo banco, snapshots, auditoria e Outbox; Event Sourcing completo fica fora da primeira entrega.

## Consequências

Há hosts separados e contratos HTTP a versionar. MVC não reutiliza o DbContext. A API concentra autorização e regras de comandos. Não criar Angular, telas ou infraestrutura completa antes de validar a importação.

A implantação contínua do Worker e a autenticação entre hosts dependem de decisões específicas antes de sua implementação.

A solução usa o formato `.slnx`. A criação e evolução do banco usam exclusivamente migrations EF Core revisadas e aplicadas explicitamente.

A solução, SDK, propriedades de build e manifesto de ferramentas do backend ficam dentro de `backend/`. A solução não inclui o MVC. O frontend mantém configuração .NET própria para o MVC; arquivos de orquestração compartilhada permanecem na raiz.
