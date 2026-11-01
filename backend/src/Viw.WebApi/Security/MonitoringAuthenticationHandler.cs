using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Viw.WebApi.Security;

public sealed class MonitoringOptions
{
    public string ApiKey { get; set; } = "";
}

// Chave exclusivamente para consulta servidor-a-servidor; não é login de usuário do Admin.
public sealed class MonitoringAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> schemes,
    ILoggerFactory logger, UrlEncoder encoder, IOptions<MonitoringOptions> monitoring, IHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemes, logger, encoder)
{
    public const string SchemeName = "ImportMonitor";
    public const string Policy = "Imports.Read";
    public const string HeaderName = "X-Viw-Monitor-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configured = monitoring.Value.ApiKey;
        if (string.IsNullOrEmpty(configured) || !Request.Headers.TryGetValue(HeaderName, out var values))
            return Task.FromResult(AuthenticateResult.NoResult());
        if ((!Request.IsHttps && !environment.IsDevelopment()) || values.Count != 1 || values[0] is not { Length: >= 32 and <= 512 } provided)
            return Task.FromResult(AuthenticateResult.Fail("Credencial inválida ou transporte inseguro."));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash))
            return Task.FromResult(AuthenticateResult.Fail("Credencial inválida."));
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "import-monitor"), new Claim("permission", "imports.read")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = SchemeName;
        Response.Headers.CacheControl = "no-store";
        return Task.CompletedTask;
    }
}
