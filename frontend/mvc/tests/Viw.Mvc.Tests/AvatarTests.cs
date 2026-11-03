using System.Net;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Viw.Mvc.Services;
using Viw.Mvc.TagHelpers;

namespace Viw.Mvc.Tests;

public sealed class AvatarTests
{
    [Theory]
    [InlineData("players")]
    [InlineData("sourceplayers")]
    [InlineData("teams")]
    [InlineData("editions")]
    public void Media_avatars_preserve_real_images_and_declare_private_gray_fallback(string kind)
    {
        // Arrange
        var id = Guid.NewGuid();
        var helper = new AvatarTagHelper { EntityId = id, Kind = kind, Name = "Nome reservado" };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("viw-avatar", new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        // Act
        helper.Process(context, output);
        var html = WebUtility.HtmlDecode(output.Content.GetContent());
        // Assert
        html.ShouldContain($"src=\"/imagens/{kind}/{id}\"");
        html.ShouldContain($"data-media-placeholder=\"{PortalFormat.PlaceholderAvatar}\"");
        html.ShouldContain("referrerpolicy=\"no-referrer\"");
    }

    [Fact]
    public void Avatar_without_a_media_identifier_uses_placehold_instead_of_a_local_image()
    {
        // Arrange
        var helper = new AvatarTagHelper { Name = "Nome reservado" };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("viw-avatar", new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        // Act
        helper.Process(context, output);
        var html = WebUtility.HtmlDecode(output.Content.GetContent());
        // Assert
        html.ShouldContain($"src=\"{PortalFormat.PlaceholderAvatar}\"");
        html.ShouldNotContain("Nome reservado");
    }
}
