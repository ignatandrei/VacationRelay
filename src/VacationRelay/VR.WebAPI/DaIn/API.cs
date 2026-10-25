using EFCore_DaIn;
using EFCore_DaIn_10;
using EFCore_Dain_Settings;
using EFCore_DaInLoader;
using Microsoft.AspNetCore.Mvc;
using VR.WebAPI;

namespace VR.DaIn;

public static class APIDain
{
    public static string PluginsDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");

    public static void RegisterDain(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IDainEF_Data, DainEF_Data>();
    }
    public static void MapDain(this WebApplication app)
    {
        var grp = app.MapGroup("/api/dain/plugins");
        grp.MapGet("/exists", () =>
        {
            try
            {
                return Results.Ok(ExistsPlugins());
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Checking for plugins failed");
                return Results.Problem("Failed to check for plugins. Check the API logs.");
            }
        });
        grp.MapGet("/all", () =>
        {
            var availablePlugins = GetPlugins();
            if (availablePlugins.Count == 0)
            {
                return Results.NotFound("No plugins found");
            }
            return Results.Ok(availablePlugins.Select(plugin => new
            {
                plugin.ProviderName,
                plugin.Description,
                plugin.Version,
                plugin.NeedServer
            }));
        });

        grp.MapPost("/loadFromInternet", async () =>
        {
            return await ExtractPlugins(PluginsDirectory);
        });
        grp.MapGet("/defaultPlugin", async () =>
        {
            try
            {
                var (plugin, connection) = await EfCorePluginLoader_10.RetrieveLatestChosenPlugin(PluginsDirectory);
                if (plugin == null)
                {
                    return Results.NotFound("Plugin not found");
                }
                return Results.Ok(plugin.ProviderName);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Loading the chosen plugin failed");
                return Results.Problem("Failed to load the chosen plugin. Check the API logs.");
            }
        });
        grp.MapPost("/testConnection", async ([FromBody] DainEF_Data data, CancellationToken ct) =>
        {
            var error = ValidateConnection(data);
            if (error != null)
            {
                return Results.BadRequest(new { Message = error });
            }

            var plugin = GetPlugins().FirstOrDefault(plugin => plugin.ProviderName == data.PluginName);
            if (plugin == null)
            {
                return Results.NotFound(new { Message = "The selected plugin is not available." });
            }

            try
            {
                var success = await plugin.TestConnectionAsync(data.ConnectionString, ct);
                return Results.Ok(new
                {
                    Success = success,
                    Message = success ? "Connection successful." : "The plugin could not connect to the database."
                });
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                app.Logger.LogError(ex, "Connection test failed for plugin {PluginName}", data.PluginName);
                return Results.Problem("Connection testing failed. Check your connection settings and the API logs.");
            }
        });

        grp.MapPost("/defaultPlugin", async ([FromBody] DainEF_Data data) =>
        {
            var error = ValidateConnection(data);
            if (error != null)
            {
                return Results.BadRequest(new { Message = error });
            }

            if (!GetPlugins().Any(plugin => plugin.ProviderName == data.PluginName))
            {
                return Results.NotFound(new { Message = "The selected plugin is not available." });
            }

            try
            {
                var b = await EfCorePluginLoader_10.SaveChosenPlugin(data, PluginsDirectory);
                if (b)
                {
                    return Results.Ok();
                }
                return Results.Problem("Failed to save plugin");
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Saving plugin {PluginName} failed", data.PluginName);
                return Results.Problem("Failed to save the selected plugin. Check the API logs.");
            }
        });

        app.MapVrData();
    }

    private static string? ValidateConnection(DainEF_Data data)
    {
        if (string.IsNullOrWhiteSpace(data.PluginName))
        {
            return "A plugin is required.";
        }
        return null;
    }
    private static bool ExistsPlugins()
    {
        var assemblies = EfCorePluginLoader_10.DiscoverPluginAssemblies(PluginsDirectory);
        if (assemblies == null)
        {
            return false;
        }

        return assemblies.Any();
    }
    private static IReadOnlyList<IEFCore_DatabasePlugin> GetPlugins()
    {
        var assemblies = EfCorePluginLoader_10.DiscoverPluginAssemblies(PluginsDirectory);
        if (assemblies == null)
        {
            return [];
        }

        return [.. assemblies
            .SelectMany(assembly => EfCorePluginLoader_10.LoadFromAssembly(assembly, isUnloadable: false))
            .DistinctBy(plugin => plugin.ProviderName)
            .OrderBy(plugin => plugin.ProviderName)];
    }
    public static async Task<HashSet<Version>> ExtractPlugins(string pluginsDirectory)
    {
        Console.WriteLine("extracting plugins");
        ProvidersIndexLoaders loaders = new("https://ignatandrei.github.io/");
        var providers = await loaders.LoadFromUrlAsync();
        ArgumentNullException.ThrowIfNull(providers);
        var efCoreVersions =new HashSet<Version>(providers.VersionsEFCore());
        foreach (var version in efCoreVersions)
        {

            Console.WriteLine($"EF Core version: {version}");
            var runtimeOnCurrentOS = providers.LoadProviders(version);
            foreach (var runtime in runtimeOnCurrentOS)
            {
                string pluginDirectory = Path.Combine(pluginsDirectory, version.ToString(), runtime.Name!);

                string extractedPath = Path.Combine(pluginDirectory, runtime.Rid!);

                if (Directory.Exists(extractedPath))
                {
                    Console.WriteLine($"Plugin {runtime.Name} for {runtime.Rid} already downloaded. Skipping...");
                    continue;
                }
                await loaders.SaveProviderFromUrl(runtime, version, cancellationToken: CancellationToken.None);

            }
        }
        return efCoreVersions;
    }
}