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

Consulte [operação do Admin](../../docs/development/admin.md) para migrations, primeiro acesso e permissões.
