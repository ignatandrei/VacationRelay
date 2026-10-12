using System.Net;
using System.Net.Http.Json;
using EFCore_StartPlugins.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore_StartPlugins.Services;

public sealed class PluginApiClient([FromKeyedServices(Program.apiNameConfig)] HttpClient http)
{
    public string BaseAPIUrl => http.BaseAddress?.ToString() ??"";
    public async Task<PluginInfo[]> GetPluginsAsync()
    {
        using var response = await http.GetAsync($"api/dain/plugins/all");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PluginInfo[]>()
            ?? throw new InvalidOperationException("The plugin API returned an empty response.");
    }
    public async Task<bool> ExistsPluginsAsync()
    {
        using var response = await http.GetAsync($"api/dain/plugins/exists");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<string?> GetChosenPluginAsync()
    {
        using var response = await http.GetAsync($"api/dain/plugins/defaultPlugin");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<string>();
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(PluginConnection connection)
    {
        using var response = await http.PostAsJsonAsync($"api/dain/plugins/testConnection", connection);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ConnectionTestResult>()
            ?? throw new InvalidOperationException("The connection test returned an empty response.");
    }

    public async Task SaveAsync(PluginConnection connection)
    {
        using var response = await http.PostAsJsonAsync($"api/dain/plugins/defaultPlugin", connection);
        await EnsureSuccessAsync(response);
    }

    

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = $"The plugin API returned HTTP {(int)response.StatusCode}.";
        if (response.Content.Headers.ContentType?.MediaType is "application/json" or "application/problem+json")
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            message = error?.Message ?? error?.Detail ?? error?.Title ?? message;
        }
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private sealed record ClientConfiguration(string ApiBaseUrl);
    private sealed record ApiError(string? Message, string? Detail, string? Title);
}
