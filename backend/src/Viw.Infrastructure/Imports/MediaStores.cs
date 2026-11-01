using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Viw.Application.Imports;

namespace Viw.Infrastructure.Imports;

public sealed class LocalMediaStore(IOptions<MediaOptions> options) : IMediaStore
{
    public async Task PutAsync(string key, byte[] bytes, string contentType, CancellationToken ct)
    {
        if (Path.GetFileName(key) != key) throw new ArgumentException("Chave de arquivo inválida.", nameof(key));
        var root = Path.GetFullPath(options.Value.LocalPath);
        Directory.CreateDirectory(root);
        var destination = Path.Combine(root, key);
        if (File.Exists(destination)) return;
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, ct);
            try { File.Move(temporary, destination, false); }
            catch (IOException) when (File.Exists(destination)) { }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public sealed class R2MediaStore(IOptions<MediaOptions> options) : IMediaStore
{
    public async Task PutAsync(string key, byte[] bytes, string contentType, CancellationToken ct)
    {
        var settings = options.Value;
        using var client = new AmazonS3Client(new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey),
            new AmazonS3Config { ServiceURL = settings.R2Endpoint, ForcePathStyle = true, AuthenticationRegion = "auto" });
        using var stream = new MemoryStream(bytes, writable: false);
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = settings.Bucket, Key = "media/" + key, InputStream = stream, ContentType = contentType,
            DisablePayloadSigning = true, DisableDefaultChecksumValidation = true, UseChunkEncoding = false
        }, ct);
    }
}
