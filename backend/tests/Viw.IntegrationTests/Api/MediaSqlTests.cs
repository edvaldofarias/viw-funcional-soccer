using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Viw.Domain.Competition;
using Viw.Infrastructure.Imports;
using Viw.IntegrationTests.Support;
using Viw.WebApi.Controllers;

namespace Viw.IntegrationTests.Api;

public sealed class MediaSqlTests
{
    [SqlServerFact]
    public async Task Image_prefers_own_storage_and_falls_back_to_recorded_firebase_url()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var d = database.Data;
        var path = Path.Combine(Path.GetTempPath(), "viw-media-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try {
            var bytes = new byte[] {137,80,78,71,13,10,26,10};
            var asset = new MediaAsset { StorageKey="fixture.png",ContentHash="fixture",ContentType="image/png",SizeBytes=bytes.Length };
            db.Add(asset);await db.SaveChangesAsync();
            var team=await db.TeamEntryRecords.FindAsync(d.Home.Id);team!.SourceImageAssetId=asset.Id;
            team.SourceImageUrl="https://firebasestorage.googleapis.com/fixture.png";await db.SaveChangesAsync();
            await File.WriteAllBytesAsync(Path.Combine(path,asset.StorageKey),bytes);
            var options=Options.Create(new MediaOptions {LocalPath=path});
            var controller=new MediaController(db,new MediaReader(options),options) {ControllerContext=new ControllerContext {HttpContext=new DefaultHttpContext()}};
            // Act
            var stored=await controller.Get("teams",team.Id,null,ct:default);
            // Assert
            stored.ShouldBeOfType<FileContentResult>().FileContents.ShouldBe(bytes);
            // Act: storage file missing, keep the original URL.
            File.Delete(Path.Combine(path,asset.StorageKey));
            var fallback=await controller.Get("teams",team.Id,null,ct:default);
            // Assert
            fallback.ShouldBeOfType<RedirectResult>().Url.ShouldBe(team.SourceImageUrl);
            // Act: arbitrary external hosts must not be used.
            team.SourceImageUrl="https://example.invalid/private";await db.SaveChangesAsync();
            var blocked=await controller.Get("teams",team.Id,null,ct:default);
            // Assert
            blocked.ShouldBeOfType<NotFoundResult>();
        } finally {Directory.Delete(path,true);}
    }
}
