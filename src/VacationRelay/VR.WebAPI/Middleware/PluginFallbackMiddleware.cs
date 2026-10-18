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
    private bool? _hasPlugin = false;
    private readonly string _fallback;
    /// <summary>
    /// reading Dain:FallbackUrl from configuration
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="configuration"></param>
    public PluginFallbackMiddleware(ILogger<PluginFallbackMiddleware> logger, IConfiguration configuration)
    {
        _logger = logger;
        _fallback = configuration["Dain:NoPluginChosenUrl"] ??"/dain";
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Always allow plugin management endpoints through
        if (path.StartsWith("/api/dain/plugins", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return; 
        }
        if(_hasPlugin.HasValue)
        {
            if(_hasPlugin.Value)
            {   
                await next(context);
                return;
            }            
        }
        try
        {
            var (plugin, connection) = await EfCorePluginLoader_10.RetrieveLatestChosenPlugin(VR.DaIn.API.PluginsDirectory);
            _hasPlugin = (plugin != null);
            if (plugin != null)
            {
                // chosen plugin exists, continue normal pipeline
                await next(context);
                return;
            }
        }
        catch (Exception ex)
        {
            // Log and continue the pipeline rather than fail the request
            _logger.LogError(ex, "Failed to determine chosen plugin");
            await next(context);
            return;
        }

        // No plugin selected / available => redirect to fallback URL
        var original = (context.Request.Path + context.Request.QueryString).ToString();
        var target = _fallback.TrimEnd('/') +"?"+ original;
        _logger.LogInformation("Redirecting request {RequestPath} to fallback {Fallback}", original, target);
        context.Response.Redirect(target, permanent: false);
    }
}
