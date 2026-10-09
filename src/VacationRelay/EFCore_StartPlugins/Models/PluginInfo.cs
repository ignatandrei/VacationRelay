using System.ComponentModel.DataAnnotations;

namespace EFCore_StartPlugins.Models;

public sealed record PluginInfo(string ProviderName, string Description, string Version, bool NeedServer);

public sealed class PluginConnection
{
    [Required(ErrorMessage = "Select a plugin.")]
    public string PluginName { get; set; } = "";

    public string ConnectionString { get; set; } = "";

    public string DatabaseName { get; set; } = "";
}

public sealed record ConnectionTestResult(bool Success, string Message);
