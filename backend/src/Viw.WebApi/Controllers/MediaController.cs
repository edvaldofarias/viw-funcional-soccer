using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Viw.Infrastructure.Imports;
using Viw.Infrastructure.Persistence;

namespace Viw.WebApi.Controllers;

/// <summary>Imagens das entidades públicas: prioriza o storage próprio e redireciona para a origem cadastrada quando indisponível.</summary>
[ApiController, Route("api/media")]
public sealed class MediaController(ViwDbContext db, MediaReader reader, IOptions<MediaOptions> options) : ControllerBase
{
    /// <summary>Retorna foto/escudo/capa do jogador, equipe ou campeonato; 302 para Firebase como alternativa e 404 sem imagem.</summary>
    /// <remarks>kind aceita players, teams ou editions. editionId seleciona a foto histórica do jogador; cover seleciona a capa da edição. Não aceita URLs ou chaves de arquivo fornecidas pelo cliente.</remarks>
    [HttpGet("{kind}/{id:guid}")]
    public async Task<IActionResult> Get(string kind, Guid id, Guid? editionId, bool cover = false, CancellationToken ct = default)
    {
        Guid? assetId = null; string? source = null;
        if (kind == "players") {
            var photos = await (from link in db.PlayerIdentityLinkRecords.AsNoTracking()
                join player in db.SourcePlayerRecords.AsNoTracking() on link.SourcePlayerId equals player.Id
                join edition in db.ChampionshipEditionRecords.AsNoTracking() on player.EditionId equals edition.Id
                where link.PlayerId == id && link.UnlinkedAt == null && (editionId == null || player.EditionId == editionId)
                orderby edition.Year descending, edition.Semester descending, player.Id
                select new { player.SourceImageAssetId, player.SourceImageUrl }).ToListAsync(ct);
            var photo = photos.FirstOrDefault(x => x.SourceImageAssetId != null || x.SourceImageUrl != null);
            assetId = photo?.SourceImageAssetId; source = photo?.SourceImageUrl;
            if (editionId == null) assetId = await db.PlayerRecords.Where(x => x.Id == id).Select(x => x.OfficialImageId).FirstOrDefaultAsync(ct) ?? assetId;
        } else if (kind == "sourceplayers") {
            var player=await db.SourcePlayerRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);
            assetId=player?.SourceImageAssetId;source=player?.SourceImageUrl;
        } else if (kind == "teams") {
            var team = await db.TeamEntryRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            assetId = team?.OfficialImageId ?? team?.SourceImageAssetId; source = team?.SourceImageUrl;
        } else if (kind == "editions") {
            var edition = await db.ChampionshipEditionRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            assetId = cover ? edition?.SourceCoverAssetId : edition?.SourceImageAssetId;
            source = cover ? edition?.SourceCoverUrl : edition?.SourceImageUrl;
        } else return NotFound();
        var asset = assetId == null ? null : await db.MediaAssetRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assetId, ct);
        if (asset != null) {
            try {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(2));
                var bytes = await reader.ReadAsync(asset.StorageKey, timeout.Token);
                if (bytes != null && MediaImportService.Detect(bytes).ContentType == asset.ContentType) {
                    Response.Headers.CacheControl = "public,max-age=300";
                    Response.Headers["X-VIW-Image-Source"] = "storage";
                    return File(bytes, asset.ContentType);
                }
            } catch (Exception error) when (error is not OperationCanceledException || !ct.IsCancellationRequested) { /* A origem permanece disponível durante falha do storage. */ }
        }
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme == "https" && options.Value.AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase)) {
            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-VIW-Image-Source"] = "firebase";
            return Redirect(source!);
        }
        return NotFound();
    }
}
