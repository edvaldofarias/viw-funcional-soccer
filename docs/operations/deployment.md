# Publicação e operação

Destino planejado: SmarterASP.NET Premium para MVC, API e SQL Server. Storage de imagens em R2. Não há ambiente novo provisionado por esta documentação.

## Hosts e roteamento

Publicar MVC e API como aplicações distintas. Planejar mapeamento do domínio público, `/api` e futuro `/admin` no IIS. Validar configuração, caminhos e permissões no ambiente real antes de escolher entre mesma origem e subdomínios.

## Worker

Confirmar execução de processo independente, limites de requisição, intervalos do agendador e reciclagem. Não depender de processo permanente dentro do IIS compartilhado. Se necessário, usar lotes autenticados e retomáveis na API ou hospedar o Worker externamente. Credenciais do agendador não devem ir em URLs registradas em logs.

## Pipeline de publicação

1. Validar testes e builds dos componentes alterados.
2. Gerar pacotes separados por host.
3. Fazer backup e executar migrações por um responsável único.
4. Publicar versões compatíveis de API e clientes.
5. Verificar saúde, consulta pública e importação controlada.
6. Confirmar último sucesso por edição e observar falhas após publicação.

Rollback do pacote não desfaz migrações automaticamente. Planejar compatibilidade e recuperação dos dados antes de mudanças destrutivas.

## Operação

Monitorar falhas repetidas, tarefas abandonadas, atraso de atualização, concorrência e consumo de storage. Manter backups de banco e mídia com restauração testada. Separar detalhes operacionais autenticados de health checks públicos mínimos.

Preservar o site La Liga existente durante o desenvolvimento desta plataforma. Migração de dados e domínio exige plano próprio; não substituir produção como efeito colateral de preparar o repositório.
