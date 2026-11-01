namespace Viw.Infrastructure.Imports;

public sealed class MediaOptions
{
    public bool Enabled { get; set; } = true;
    public string Driver { get; set; } = "Local";
    public string LocalPath { get; set; } = "data/media";
    public int MaxBytes { get; set; } = 10 * 1024 * 1024;
    public string R2Endpoint { get; set; } = "";
    public string Bucket { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
    public string[] AllowedHosts { get; set; } = ["firebasestorage.googleapis.com", "storage.googleapis.com"];
}
