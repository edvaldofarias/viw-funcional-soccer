# Frontend

- `Viw.Mvc/`: futuro projeto ASP.NET Core MVC com Razor Views para copas, Wiki e acompanhamento autorizado das importações.
- `viw-admin/`: futuro projeto Angular administrativo.
- `tests/`: testes de apresentação e navegação.

MVC e Angular consomem a Web API. O MVC não acessa SQL Server nem executa o Worker. A tela de acompanhamento do Worker será restrita; não expõe logs ou dados de operação publicamente.

O projeto MVC foi criado e consome o endpoint inicial da API; Angular permanece reservado. Consulte [rotas e aplicações](../docs/architecture/applications-and-routes.md).
