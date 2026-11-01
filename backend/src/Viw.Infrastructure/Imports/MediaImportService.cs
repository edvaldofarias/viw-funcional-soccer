using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viw.Application.Imports;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Imports;

public sealed class MediaImportService(ViwDbContext db, ImportLeaseManager leases, HttpClient http,
    IMediaStore store, IOptions<MediaOptions> options, ILogger<MediaImportService> logger)
{
    public async Task<int> ImportEditionAsync(Guid editionId, LeaseToken lease, CancellationToken ct)
    {
        if (!options.Value.Enabled) return 0;
        db.ChangeTracker.Clear();
        var targets = new List<MediaTarget>();
        foreach (var player in await db.SourcePlayerRecords.AsNoTracking().Where(x => x.EditionId == editionId && x.SourceImageUrl != null).ToListAsync(ct))
            targets.Add(new MediaTarget("player", player.Id, player.SourceImageUrl!, player.SourceImageAssetId));
        foreach (var team in await db.TeamEntryRecords.AsNoTracking().Where(x => x.EditionId == editionId && x.SourceImageUrl != null).ToListAsync(ct))
            targets.Add(new MediaTarget("team", team.Id, team.SourceImageUrl!, team.SourceImageAssetId));
        var edition = await db.ChampionshipEditionRecords.AsNoTracking().SingleAsync(x => x.Id == editionId, ct);
        if (edition.SourceImageUrl is not null) targets.Add(new MediaTarget("edition", edition.Id, edition.SourceImageUrl, edition.SourceImageAssetId));
        if (edition.SourceCoverUrl is not null) targets.Add(new MediaTarget("cover", edition.Id, edition.SourceCoverUrl, edition.SourceCoverAssetId));
        var failures = 0;
        foreach (var target in targets)
        {
            try { await ImportAsync(target, lease, ct); }
            catch (Exception error) when (error is not ImportLeaseLostException && (error is not OperationCanceledException || !ct.IsCancellationRequested))
            { failures++; logger.LogWarning("Imagem pendente para {Kind} {Id}; tipo {ErrorType}.", target.Kind, target.Id, error.GetType().Name); }
        }
        return failures;
    }

    private async Task ImportAsync(MediaTarget target, LeaseToken lease, CancellationToken ct)
    {
        if (target.AssetId is not null && await db.MediaAssetRecords.AsNoTracking().AnyAsync(x => x.Id == target.AssetId && x.SourceUrl == target.Url, ct)) return;
        if (!Uri.TryCreate(target.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !options.Value.AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Host de imagem não permitido.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        var bytes = await CopaFacilSource.ReadLimitedAsync(response.Content, options.Value.MaxBytes, timeout.Token);
        var (extension, contentType) = Detect(bytes);
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var key = hash + extension;
        // Content-addressed objects can safely be retried after a crash between upload and DB commit.
        await store.PutAsync(key, bytes, contentType, ct);
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await leases.AssertAndRenewAsync(lease, ct);
        var asset = await db.MediaAssetRecords.SingleOrDefaultAsync(x => x.StorageKey == key, ct);
        if (asset is null)
        {
            asset = new MediaAsset { StorageKey = key, ContentHash = hash, ContentType = contentType,
                SizeBytes = bytes.Length, SourceUrl = target.Url, IsPublic = false };
            db.Add(asset);
        }
        switch (target.Kind)
        {
            case "player": (await db.SourcePlayerRecords.SingleAsync(x => x.Id == target.Id, ct)).SourceImageAssetId = asset.Id; break;
            case "team": (await db.TeamEntryRecords.SingleAsync(x => x.Id == target.Id, ct)).SourceImageAssetId = asset.Id; break;
            case "edition": (await db.ChampionshipEditionRecords.SingleAsync(x => x.Id == target.Id, ct)).SourceImageAssetId = asset.Id; break;
            case "cover": (await db.ChampionshipEditionRecords.SingleAsync(x => x.Id == target.Id, ct)).SourceCoverAssetId = asset.Id; break;
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public static (string Extension, string ContentType) Detect(byte[] data)
    {
        if (data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return (".png", "image/png");
        if (data.Length >= 3 && data[0] == 255 && data[1] == 216 && data[2] == 255) return (".jpg", "image/jpeg");
        if (data.Length >= 12 && System.Text.Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(data, 8, 4) == "WEBP") return (".webp", "image/webp");
        if (data.Length >= 6 && System.Text.Encoding.ASCII.GetString(data, 0, 6) is "GIF87a" or "GIF89a") return (".gif", "image/gif");
        throw new InvalidDataException("Formato de imagem não permitido.");
    }

    private sealed record MediaTarget(string Kind, Guid Id, string Url, Guid? AssetId);
}
