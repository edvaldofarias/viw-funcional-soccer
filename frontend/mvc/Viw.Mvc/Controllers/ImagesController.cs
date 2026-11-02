using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

/// <summary>Entrega imagens pela origem pública do portal, preservando a alternativa do Firebase definida pela API.</summary>
public sealed class ImagesController(IHttpClientFactory clients, IOptions<ViwApiOptions> options) : Controller
{
    [HttpGet("/imagens/{kind}/{id:guid}")]
    public async Task<IActionResult> Image(string kind, Guid id, Guid? editionId, bool cover = false, CancellationToken ct = default)
    {
        if (kind is not ("players" or "teams" or "editions")) return NotFound();
        var path = $"api/media/{kind}/{id:D}?cover={cover}" + (editionId is {} edition ? $"&editionId={edition:D}" : "");
        var client = clients.CreateClient("media");
        using var response = await client.GetAsync(new Uri(new Uri(options.Value.BaseUrl), path), ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Redirect && response.Headers.Location is {} location && location.Scheme == "https") return Redirect(location.AbsoluteUri);
        if (!response.IsSuccessStatusCode) return NotFound();
        var type = response.Content.Headers.ContentType?.MediaType;
        if (type is not ("image/png" or "image/jpeg" or "image/gif" or "image/webp")) return NotFound();
        Response.Headers.CacheControl = "public,max-age=300";
        return File(await response.Content.ReadAsByteArrayAsync(ct), type);
    }
}
