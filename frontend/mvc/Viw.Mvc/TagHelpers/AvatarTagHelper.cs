using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Viw.Mvc.Services;
namespace Viw.Mvc.TagHelpers;

/// <summary>Avatar com cópia própria, alternativa Firebase pela API e iniciais se ambas falharem.</summary>
[HtmlTargetElement("viw-avatar")]
public sealed class AvatarTagHelper : TagHelper
{
    public string Kind { get; set; } = "players";
    public Guid? EntityId { get; set; }
    public Guid? EditionId { get; set; }
    public string? Name { get; set; }
    public string Class { get; set; } = "avatar";
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span"; output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", Class + " media-avatar");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Content.SetContent(PortalFormat.Initials(Name));
        if (EntityId is not {} id || Kind is not ("players" or "sourceplayers" or "teams" or "editions")) return;
        var image = new TagBuilder("img"); image.Attributes["src"] = $"/imagens/{Kind}/{id:D}" + (EditionId is {} edition ? $"?editionId={edition:D}" : "");
        image.Attributes["alt"] = ""; image.Attributes["loading"] = "lazy"; image.Attributes["data-media-avatar"] = "";
        output.Content.AppendHtml(image);
    }
}
