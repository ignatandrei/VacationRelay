using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EFCore_StartPlugins;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
await LoadConfigurationAsync(builder);
await AddDIHttp(builder);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<EFCore_StartPlugins.Services.PluginApiClient>();


await builder.Build().RunAsync();
partial class Program
{
    public const string apiNameConfig = "api_host";
    public static async Task LoadConfigurationAsync(WebAssemblyHostBuilder builder)
    {
        using var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
        var response = await http.GetAsync("appsettings.json");
        var json = await response.Content.ReadAsStringAsync();
        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();
        var dict = new Dictionary<string, string?> { };

        foreach (var kvp in config.AsEnumerable())
        {
            dict[kvp.Key] = kvp.Value;
        }
        builder.Configuration.AddInMemoryCollection(dict.ToArray());

    }
    public static async Task<bool> AddDIHttp(WebAssemblyHostBuilder builder)
    {
        var hostApi = builder.Configuration[apiNameConfig];
        if (string.IsNullOrEmpty(hostApi))
        {
            hostApi = builder.HostEnvironment.BaseAddress;
            if (!hostApi.EndsWith("/"))
            {
                hostApi += "/";
            }
            var dict = new Dictionary<string, string?> { { apiNameConfig, hostApi } };
            builder.Configuration.AddInMemoryCollection(dict.ToArray());
        }
        builder.Services.AddKeyedScoped(apiNameConfig, (sp, _) => new HttpClient { BaseAddress = new Uri(hostApi) });
        builder.Services.AddKeyedScoped("local_host", (sp, _) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        return true;
    }
}