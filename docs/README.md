# Documentação

Esta documentação descreve a arquitetura acordada e os critérios de implementação. A fundação .NET está disponível; as funcionalidades de negócio permanecem planejadas.

| Documento | Conteúdo |
| --- | --- |
| [Arquitetura](architecture/overview.md) | Componentes, dependências e dados |
| [Aplicações e rotas](architecture/applications-and-routes.md) | MVC, API, Angular e segurança |
| [Modelo do banco](architecture/database-model.md) | Tabelas, integridade e limitações |
| [Identidade e premiações](architecture/domain.md) | Modelo e autoridade dos dados |
| [Decisão inicial](decisions/0001-repository-and-applications.md) | Organização e escolhas aceitas |
| [Modelo de decisão](decisions/template.md) | Registro de decisões futuras |
| [Copa Fácil](integrations/copa-facil.md) | Descoberta e mapeamento da origem |
| [Worker](worker/import-pipeline.md) | Execução, recuperação e aceite |
| [Desenvolvimento](development/getting-started.md) | Preparação e configuração |
| [Banco e migrations](development/database.md) | Criação controlada do banco com EF Core |
| [Convenções de testes](development/testing.md) | AAA, fixtures, builders e mocks |
| [Operação](operations/deployment.md) | Publicação, agendamento e backups |
| [Roadmap](planning/roadmap.md) | Entregas e pendências |

Atualizar os documentos junto das mudanças de comportamento. Registrar decisões relevantes em `decisions/`, distinguindo proposta, decisão aceita e implementação concluída. Não incluir IDs reais das edições nem segredos nos exemplos.

- [Executar Worker no Windows, Docker e storage](worker/running.md)
