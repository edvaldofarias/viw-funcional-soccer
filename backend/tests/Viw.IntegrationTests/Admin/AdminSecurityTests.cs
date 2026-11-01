using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Viw.Application.Admin;
using Viw.Contracts.Admin;

namespace Viw.IntegrationTests.Admin;

public sealed class AdminSecurityTests
{
    private static WebApplicationFactory<Program> Factory(Mock<IAdminService>? service=null,bool testAuthentication=false,string environment="Development")=>
        new WebApplicationFactory<Program>().WithWebHostBuilder(web=>web.UseEnvironment(environment)
        .ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:ViwDatabase"]="",["Monitoring:ApiKey"]="test-monitor-key-for-tests-only-123456789" }))
        .ConfigureServices(s=>{
            if(service!=null){s.RemoveAll<IAdminService>();s.AddSingleton(service.Object);}
            if(testAuthentication){s.AddTransient<TestAdminHandler>();s.PostConfigure<AuthenticationOptions>(o=>o.SchemeMap[IdentityConstants.ApplicationScheme].HandlerType=typeof(TestAdminHandler));}
        }));

    [Theory]
    [InlineData("/api/admin/players")]
    [InlineData("/api/admin/sources")]
    [InlineData("/api/admin/award-definitions")]
    [InlineData("/api/admin/audit")]
    [InlineData("/api/admin/users")]
    public async Task Anonymous_or_monitoring_key_cannot_access_admin(string path)
    {
        // Arrange
        using var factory=Factory();using var client=factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Viw-Monitor-Key","test-monitor-key-for-tests-only-123456789");
        // Act
        using var response=await client.GetAsync(path);
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.Location.ShouldBeNull();
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    }
    [Fact]
    public async Task Editor_cannot_manage_accounts()
    {
        // Arrange
        using var factory=Factory(testAuthentication:true);using var client=factory.CreateClient();client.DefaultRequestHeaders.Add("X-Test-Role","Editor");
        // Act
        using var response=await client.GetAsync("/api/admin/users");
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
    [Fact]
    public async Task Mutation_without_csrf_does_not_reach_service()
    {
        // Arrange
        var service=new Mock<IAdminService>(MockBehavior.Strict);using var factory=Factory(service,true);using var client=factory.CreateClient();client.DefaultRequestHeaders.Add("X-Test-Role","Editor");
        // Act
        using var response=await client.PutAsJsonAsync("/api/admin/players/"+Guid.NewGuid(),new PersonCommand {Name="Léo Reis",Slug="leo-reis",Reason="Correção oficial",Version="AAAAAAAAAAA="});
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);service.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task Valid_mutation_uses_authenticated_actor_and_csrf()
    {
        // Arrange
        var id=Guid.NewGuid();var service=new Mock<IAdminService>(MockBehavior.Strict);
        service.Setup(x=>x.UpdatePersonAsync(false,id,It.Is<PersonCommand>(c=>c.Name=="Léo Reis"),"test-actor",It.IsAny<CancellationToken>())).ReturnsAsync(new PersonRow(id,"Léo Reis","leo-reis",null,"AAAAAAAAAAA="));
        using var factory=Factory(service,true);using var client=factory.CreateClient();client.DefaultRequestHeaders.Add("X-Test-Role","Editor");
        var csrf=await client.GetFromJsonAsync<CsrfResponse>("/api/admin/session/csrf");client.DefaultRequestHeaders.Add("X-VIW-CSRF",csrf!.Token);
        // Act
        using var response=await client.PutAsJsonAsync("/api/admin/players/"+id,new PersonCommand {Name="Léo Reis",Slug="leo-reis",Reason="Correção oficial",Version="AAAAAAAAAAA="});
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);service.VerifyAll();
    }
    [Fact]
    public async Task Production_rejects_plain_http_for_admin()
    {
        // Arrange
        using var factory=Factory(environment:"Production");using var client=factory.CreateClient(new() {BaseAddress=new Uri("http://localhost"),AllowAutoRedirect=false});
        // Act
        using var response=await client.GetAsync("/api/admin/session/csrf");
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task Invalid_admin_query_is_rejected_before_service()
    {
        // Arrange
        var service=new Mock<IAdminService>(MockBehavior.Strict);using var factory=Factory(service,true);using var client=factory.CreateClient();client.DefaultRequestHeaders.Add("X-Test-Role","Editor");
        // Act
        using var response=await client.GetAsync("/api/admin/players?page=0");
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);service.VerifyNoOtherCalls();
    }
    private sealed record CsrfResponse(string Token);
    private sealed class TestAdminHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if(!Request.Headers.TryGetValue("X-Test-Role",out var role))return Task.FromResult(AuthenticateResult.NoResult());
            var claims=new[]{new Claim(ClaimTypes.NameIdentifier,"test-actor"),new Claim(ClaimTypes.Name,"Editor Teste"),new Claim(ClaimTypes.Role,role.ToString())};
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme.Name)),Scheme.Name)));
        }
    }
}
