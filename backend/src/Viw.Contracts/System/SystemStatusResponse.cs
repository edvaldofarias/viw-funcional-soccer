namespace Viw.Contracts.System;

/// <summary>Estágio funcional da aplicação; não é uma verificação de saúde do banco.</summary>
/// <param name="Name">Nome do sistema.</param>
/// <param name="Stage">Estágio funcional implementado da API.</param>
/// <param name="ImportsImplemented">Indica se o importador foi implementado; não confirma carga concluída ou Worker ativo.</param>
public sealed record SystemStatusResponse(string Name, string Stage, bool ImportsImplemented);
