using OpenAPISwaggerUI;
using VR.DaIn;
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
