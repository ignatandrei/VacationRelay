using EFCore_DaIn_10;
using Microsoft.EntityFrameworkCore;
using OpenAPISwaggerUI;
using VR.Context;
using VR.DaIn;
using VR.WebAPI.Middleware;
try
{
    await VR.DaIn.APIDain.ExtractPlugins(APIDain.PluginsDirectory);
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

var vacationRelayConnectionString = builder.Configuration.GetConnectionString("VacationRelay");
var (plugin1, connection1) = await EfCorePluginLoader_10.RetrieveLatestChosenPlugin(APIDain.PluginsDirectory);

if (plugin1 != null)
{
    builder.Services.AddTransient<VacationrelayContext>(sp =>
    {
        if(string.IsNullOrWhiteSpace(vacationRelayConnectionString))
        {
            vacationRelayConnectionString = connection1!.ConnectionString;
        }
        var opt = plugin1.GenerateDbContextOptionsBuilder<VacationrelayContext>(vacationRelayConnectionString,connection1!.DatabaseName);
        return new VacationrelayContext(opt.Options);
    });
}
//builder.Services.AddDbContext<VacationrelayContext>(options =>
//{

//    if (!string.IsNullOrWhiteSpace(vacationRelayConnectionString))
//    {
//        options.UseSqlServer(vacationRelayConnectionString);
//    }
//});

// Register middleware implementation
builder.Services.AddSingleton<PluginFallbackMiddleware>();
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
