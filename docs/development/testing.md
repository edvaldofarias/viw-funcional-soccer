# Convenções de testes

Todo teste deve explicitar `// Arrange`, `// Act` e `// Assert`. Preparação pode persistir pré-condições; o Act deve destacar a operação cuja consequência está sendo verificada. Capturar uma exceção no Act e verificar seu tipo/código no Assert mantém esse fluxo legível.

- Testes pequenos e com um comportamento por cenário; não juntar diversas regras em um único teste extenso.
- `Support/ChampionshipScenario` fornece dados sintéticos e factories para partidas, vínculos e premiações.
- `Support/SqlServerTestDatabase` cria/aplica migrations em banco descartável por teste e garante descarte. Nunca usa o catálogo original da conexão.
- `Support/ModelContextFactory` prepara metadados sem abrir conexão.
- Shouldly padroniza as asserções (`ShouldBe`, `ShouldContain`, `ShouldBeOfType`, `Should.Throw`); xUnit continua como framework de execução. Os projetos de teste importam Shouldly por `GlobalUsings.cs`.
- Moq é usado em unidade para dependências de colaboração, como `IHostApplicationLifetime` no Worker. Não simular `DbContext` para comprovar constraints SQL.
- Testes de API usam o pipeline real por `WebApplicationFactory`; testes SQL continuam dependendo de SQL Server real isolado.
- Ao adicionar uma rota pública em `/api/v1/`, atualizar a contagem em `OpenApiDocumentationTests` e garantir resumo, descrição e contrato de sucesso no documento gerado.
- Sem `ConnectionStrings__ViwTestServer`, os oito cenários SQL aparecem explicitamente como ignorados. A CI fornece esse servidor.
- Novos cenários de domínio poderão ampliar os builders; não criar interfaces ou mocks na produção apenas para satisfazer um teste.

Comentários XML das entidades do Domain apresentam seu nome e finalidade em português; nomes de classes continuam em inglês.
