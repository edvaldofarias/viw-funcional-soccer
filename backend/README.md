# Backend

Contém os futuros executores .NET `Viw.Worker` e `Viw.WebApi`, além das bibliotecas de domínio, aplicação, infraestrutura e contratos.

A implementação começa pelo Worker. Web API e Worker compartilham casos de uso e persistência, mas têm ciclos de execução separados. O Worker não deve depender de chamadas HTTP à API para importar os dados.

Estrutura reservada em `src/`; testes em `tests/`. Nenhum projeto executável foi criado nesta etapa. Consulte [a arquitetura](../docs/architecture/overview.md) e [o plano do Worker](../docs/worker/import-pipeline.md).
