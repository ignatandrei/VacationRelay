using OpenAPISwaggerUI;
using VR.DaIn;
using VR.WebAPI.Middleware;
try
{
    await VR.DaIn.API.ExtractPlugins(API.PluginsDirectory);
}
catch(Exception ex)
{
    Console.WriteLine($"Error extracting plugins: {ex.Message}");
}
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.RegisterDain();
// Register middleware implementation
builder.Services.AddTransient<PluginFallbackMiddleware>();
//var pluginUiOrigins = builder.Configuration.GetSection("PluginUiOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("PluginUi", policy =>
{
    policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(origin => true)
        .AllowCredentials()
        ;
    
}));
var app = builder.Build();
app.UseCors("PluginUi");

// Middleware that redirects requests to a fallback URL when the chosen plugin is not available.
app.UseMiddleware<PluginFallbackMiddleware>();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.UseOpenAPISwaggerUI();

//}

//app.UseHttpsRedirection();

app.MapDain();
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
