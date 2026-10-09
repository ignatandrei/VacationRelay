using System.Net;
using System.Net.Http.Json;
using EFCore_StartPlugins.Models;

namespace EFCore_StartPlugins.Services;

public sealed class PluginApiClient(HttpClient http)
{
    private Uri? apiBaseUri;

    public async Task<PluginInfo[]> GetPluginsAsync()
    {
        using var response = await http.GetAsync(await GetUriAsync("all"));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PluginInfo[]>()
            ?? throw new InvalidOperationException("The plugin API returned an empty response.");
    }

    public async Task<string?> GetChosenPluginAsync()
    {
        using var response = await http.GetAsync(await GetUriAsync("defaultPlugin"));
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<string>();
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(PluginConnection connection)
    {
        using var response = await http.PostAsJsonAsync(await GetUriAsync("testConnection"), connection);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ConnectionTestResult>()
            ?? throw new InvalidOperationException("The connection test returned an empty response.");
    }

    public async Task SaveAsync(PluginConnection connection)
    {
        using var response = await http.PostAsJsonAsync(await GetUriAsync("defaultPlugin"), connection);
        await EnsureSuccessAsync(response);
    }

    private async Task<Uri> GetUriAsync(string path)
    {
        if (apiBaseUri == null)
        {
            var configuration = await http.GetFromJsonAsync<ClientConfiguration>("client-configuration");
            if (!Uri.TryCreate(configuration?.ApiBaseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("The plugin UI host has no valid API address.");
            }
            apiBaseUri = uri;
        }
        return new Uri(apiBaseUri, $"api/dain/plugins/{path}");
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
