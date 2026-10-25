using EFCore_DaIn_10;

namespace VR.WebAPI.Middleware;

/// <summary>
/// IMiddleware that checks whether the currently chosen plugin exists. If not, it redirects
/// all requests (except those under /api/dain/plugins) to a fallback URL defined in
/// configuration at "Dain:FallbackUrl".
/// </summary>
public class PluginFallbackMiddleware : IMiddleware
{
    private readonly ILogger<PluginFallbackMiddleware> _logger;
    private static bool? _hasPlugin;
    private readonly string _fallback;

    /// <summary>
    /// reading Dain:FallbackUrl from configuration
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="configuration"></param>
    public PluginFallbackMiddleware(ILogger<PluginFallbackMiddleware> logger, IConfiguration configuration)
    {
        _logger = logger;
        _fallback =configuration["Dain:NoPluginChosenUrl"] ?? "/dain";
        _fallback= string.IsNullOrWhiteSpace(_fallback) ? "/dain" : _fallback;
        _fallback = _fallback.TrimEnd('/') ;
        
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith(_fallback, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }
        // Always allow plugin management endpoints through
        if (path.StartsWith("/api/dain/plugins", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // First request: read and await.
        if (!_hasPlugin.HasValue)
        {
            await RefreshHasPluginAsync();
        }
        else
        {
            // Next requests: use cached value immediately, refresh in background.
            _ = RefreshHasPluginAsync();
        }

        if (_hasPlugin == true)
        {
            await next(context);
            return;
        }

        
        // No plugin selected / available => redirect to fallback URL
        var original = (context.Request.Path + context.Request.QueryString).ToString();
        var target = _fallback.TrimEnd('/') + "?" + original;
        _logger.LogInformation("Redirecting request {RequestPath} to fallback {Fallback}", original, target);
        context.Response.Redirect(target, permanent: false);
    }

    private async Task RefreshHasPluginAsync()
    {
        try
        {
            var (plugin, _) = await EfCorePluginLoader_10.RetrieveLatestChosenPlugin(VR.DaIn.APIDain.PluginsDirectory);
            _hasPlugin = plugin != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine chosen plugin");
        }
    }
}
