using EFCore_DaIn_10;
using EFCore_Dain_Settings;
using EFCore_DaInLoader;

Console.WriteLine("Verifying plugins");

string pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
try
{
    //try to load the saved plugin, if it exists
    var (plugin, connection) = await EfCorePluginLoader_10.RetrieveLatestChosenPlugin();
    
    if (plugin != null)
    {
        Console.WriteLine("Chosen plugin already exists, using it " + plugin.ProviderName);
        return;
    }
    
}
catch
{
    //swallow the exception, we will try to extract the plugins
}
var discoveredAssemblies = EfCorePluginLoader_10.DiscoverPluginAssemblies(pluginsDirectory);
if(discoveredAssemblies== null || !discoveredAssemblies.Any())
{
    await ExtractPlugins(pluginsDirectory);
    discoveredAssemblies = EfCorePluginLoader_10.DiscoverPluginAssemblies(pluginsDirectory);
}
if(discoveredAssemblies == null || !discoveredAssemblies.Any())
{
    Console.WriteLine("No plugins found, exiting");
    return;
}
foreach (var assembly in discoveredAssemblies)
{
    //Console.WriteLine($"Discovered plugin assembly: {assembly}");
    var loaded = EfCorePluginLoader_10.LoadFromAssembly(assembly, isUnloadable: false);
    var first = loaded[0];
    Console.WriteLine($"Do you want to use provider {first.ProviderName} (y/N)?");
    var answer = Console.ReadLine();
    if(answer == "y" || answer == "Y")
    {
        DainEF_Data data = new DainEF_Data();
        data.PluginName = first.ProviderName;
        data.ConnectionString = "fake";
        data.DatabaseName = "fakeAgain";

        var b = await EfCorePluginLoader_10.SaveChosenPlugin(data);

        Console.WriteLine($"Using default provider {first.ProviderName}");
        break;
    }
}
static async Task ExtractPlugins(string pluginsDirectory)
{
    Console.WriteLine("extracting plugins");
    ProvidersIndexLoaders loaders = new("https://ignatandrei.github.io/");
    var providers = await loaders.LoadFromUrlAsync();
    ArgumentNullException.ThrowIfNull(providers);
    var efCoreVersions = providers.VersionsEFCore();
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
}