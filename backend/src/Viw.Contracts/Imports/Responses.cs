using Viw.Contracts.Catalog;

namespace Viw.Contracts.Imports;

/// <summary>Paginação e filtro opcional das execuções de importação.</summary>
public sealed class ImportRunRequest : PageRequest
{
    /// <summary>GUID interno opcional da edição; omitir para listar execuções de todas as edições.</summary>
    public Guid? EditionId { get; set; }
}

/// <summary>Estado persistido de uma execução de importação, sem dados privados da fonte.</summary>
/// <param name="Id">GUID interno da execução.</param>
/// <param name="EditionId">GUID interno da edição importada.</param>
/// <param name="Status">Estado: Queued, Running, Succeeded, Failed ou Cancelled.</param>
/// <param name="StartedAt">Início da execução; null se ainda não iniciada.</param>
/// <param name="CompletedAt">Conclusão da execução; null enquanto não encerrada.</param>
/// <param name="RecordsRead">Quantidade de registros lidos e confirmados até o último lote persistido.</param>
/// <param name="RecordsChanged">Quantidade de alterações de fatos registradas durante a execução; não é porcentagem de progresso.</param>
/// <param name="FailureCode">Código de falha operacional; null quando não registrado. Não inclui stack trace ou credenciais.</param>
public sealed record ImportRunResponse(Guid Id, Guid EditionId, string Status, DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt, int RecordsRead, int RecordsChanged, string? FailureCode);

/// <summary>Execução e indicadores operacionais da lease atual da edição.</summary>
/// <param name="Run">Estado persistido da execução solicitada.</param>
/// <param name="LeaseActive">Indica execução Running com lease atual da edição ainda não expirada. Não identifica o proprietário da lease.</param>
/// <param name="RecoveryPending">Indica execução Running sem lease ativa; pode exigir recuperação pelo Worker. A consulta não inicia essa recuperação.</param>
/// <param name="LeaseExpiresAt">Expiração da lease atual da edição, ou null se não cadastrada.</param>
public sealed record ImportRunDetailResponse(ImportRunResponse Run, bool LeaseActive,
    bool RecoveryPending, DateTimeOffset? LeaseExpiresAt);

/// <summary>Último checkpoint confirmado de um recurso da edição; não é histórico por execução.</summary>
/// <param name="Resource">Nome/caminho lógico do recurso confirmado, sem expor cursor, hash ou JSON original.</param>
/// <param name="UpdatedAt">Instante da última atualização confirmada desse checkpoint.</param>
public sealed record ImportCheckpointResponse(string Resource, DateTimeOffset UpdatedAt);
