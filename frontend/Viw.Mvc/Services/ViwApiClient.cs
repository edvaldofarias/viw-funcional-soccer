using System.Net.Http.Json;
using Viw.Contracts.System;

namespace Viw.Mvc.Services;

public sealed class ViwApiClient(HttpClient httpClient)
{
    public Task<SystemStatusResponse?> GetSystemStatusAsync(CancellationToken cancellationToken) =>
        httpClient.GetFromJsonAsync<SystemStatusResponse>("api/v1/system", cancellationToken);
}
