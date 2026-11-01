using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Viw.IntegrationTests.Support;
using Viw.WebApi.Admin;

namespace Viw.IntegrationTests.Admin;

public sealed class AdminIdentitySqlTests
{
    [Fact]
    public async Task Real_identity_login_cookie_csrf_password_change_and_logout_work_together()
    {
        // Arrange
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        using var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(web=>web.UseEnvironment("Development")
            .ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> {["ConnectionStrings:ViwDatabase"]=""}))
            .ConfigureServices(s=> {s.RemoveAll<DbContextOptions<AdminIdentityDbContext>>();s.RemoveAll<IDbContextOptionsConfiguration<AdminIdentityDbContext>>();s.AddDbContext<AdminIdentityDbContext>(o=>o.UseSqlite(connection));}));
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AdminIdentityDbContext>();await db.Database.EnsureCreatedAsync();
            var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            (await roles.CreateAsync(new IdentityRole<Guid>("Editor"))).Succeeded.ShouldBeTrue();
            var users=scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();var user=new AdminUser {UserName="editor@viw.test",Email="editor@viw.test"};
            (await users.CreateAsync(user,"Initial-Test!2026")).Succeeded.ShouldBeTrue();(await users.AddToRoleAsync(user,"Editor")).Succeeded.ShouldBeTrue();
        }
        using var client=factory.CreateClient();
        var before=await client.GetFromJsonAsync<Csrf>("/api/admin/session/csrf");client.DefaultRequestHeaders.Add("X-VIW-CSRF",before!.Token);
        // Act
        using var login=await client.PostAsJsonAsync("/api/admin/session/login",new {Email="editor@viw.test",Password="Initial-Test!2026"});
        using var me=await client.GetAsync("/api/admin/session");
        var token=await client.GetFromJsonAsync<Csrf>("/api/admin/session/csrf");client.DefaultRequestHeaders.Remove("X-VIW-CSRF");client.DefaultRequestHeaders.Add("X-VIW-CSRF",token!.Token);
        using var change=await client.PostAsJsonAsync("/api/admin/session/password",new {CurrentPassword="Initial-Test!2026",NewPassword="Updated-Test!2026"});
        var renewed=await client.GetFromJsonAsync<Csrf>("/api/admin/session/csrf");client.DefaultRequestHeaders.Remove("X-VIW-CSRF");client.DefaultRequestHeaders.Add("X-VIW-CSRF",renewed!.Token);
        using var logout=await client.PostAsJsonAsync("/api/admin/session/logout",new {});
        using var anonymous=await client.GetAsync("/api/admin/session");
        // Assert
        login.StatusCode.ShouldBe(HttpStatusCode.OK);me.StatusCode.ShouldBe(HttpStatusCode.OK);change.StatusCode.ShouldBe(HttpStatusCode.NoContent);logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        login.Headers.GetValues("Set-Cookie").ShouldContain(x=>x.Contains("httponly",StringComparison.OrdinalIgnoreCase));
    }
    [SqlServerFact]
    public async Task Identity_migrations_preserve_existing_competition_data()
    {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();
        var before=await database.Context.ChampionshipEditionRecords.CountAsync();
        await using var identity=new AdminIdentityDbContext(new DbContextOptionsBuilder<AdminIdentityDbContext>()
            .UseSqlServer(database.Context.Database.GetConnectionString(),sql=>sql.MigrationsHistoryTable("__AdminMigrationsHistory","admin")).Options);
        // Act
        await identity.Database.MigrateAsync();
        var migrations=await identity.Database.GetAppliedMigrationsAsync();
        // Assert
        migrations.ShouldNotBeEmpty();(await identity.Users.CountAsync()).ShouldBe(0);
        (await database.Context.ChampionshipEditionRecords.CountAsync()).ShouldBe(before);
    }
    private sealed record Csrf(string Token);
}
