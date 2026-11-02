using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Viw.Infrastructure.Imports;

/// <summary>Lê a cópia própria com limite de tempo; a origem continua sendo alternativa da apresentação.</summary>
public sealed class MediaReader(IOptions<MediaOptions> options)
{
    public async Task<byte[]?> ReadAsync(string key, CancellationToken ct)
    {
        if (Path.GetFileName(key) != key) return null;
        var settings = options.Value;
        if (settings.Driver == "Local") {
            var path = Path.Combine(Path.GetFullPath(settings.LocalPath), key);
            if (!File.Exists(path) || new FileInfo(path).Length > settings.MaxBytes) return null;
            return await File.ReadAllBytesAsync(path, ct);
        }
        using var client = new AmazonS3Client(new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey),
            new AmazonS3Config { ServiceURL = settings.R2Endpoint, ForcePathStyle = true, AuthenticationRegion = "auto" });
        using var response = await client.GetObjectAsync(new GetObjectRequest { BucketName = settings.Bucket, Key = "media/" + key }, ct);
        if (response.ContentLength > settings.MaxBytes) return null;
        using var memory = new MemoryStream();
        var buffer = new byte[8192]; int count;
        while ((count = await response.ResponseStream.ReadAsync(buffer, ct)) > 0) {
            if (memory.Length + count > settings.MaxBytes) return null;
            await memory.WriteAsync(buffer.AsMemory(0, count), ct);
        }
        return memory.ToArray();
    }
}
