# VIW Admin

Angular 21, TypeScript e sessão ASP.NET Core Identity. Todo o Angular está nesta pasta. O MVC e os testes .NET do frontend ficam em `../mvc`.

```sh
npm ci
npm start
```

Abrir `http://localhost:4200/admin/`. A API deve estar em `http://localhost:5080`; o proxy local mantém a mesma origem para cookies/CSRF. Não colocar chaves de banco, monitoramento ou senhas no Angular.

```sh
npm test
npm run build
```

O build fica em `dist/viw-admin/browser`. Publicar em `wwwroot/admin` da Web API. Docker e CI fazem esse empacotamento. Configurar domínio administrativo apontando para a API se necessário.

Todos os grids usam DataTables 3 / Bootstrap 5 com dados completos do filtro, busca e ordenação locais. A diretiva `ViwDataTable` integra a biblioteca ao ciclo de vida Angular; não inicializar DataTables diretamente nos componentes. As opções comuns ficam em `../shared/table-options.json`. Ao alterá-las, também executar `npm run vendor` em `../mvc` para regenerar o asset do portal. Auditoria e candidatos do assistente continuam paginados no servidor.

Consulte [operação do Admin](../../docs/development/admin.md) para migrations, primeiro acesso e permissões.
