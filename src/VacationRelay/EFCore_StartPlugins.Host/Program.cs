var builder = WebApplication.CreateBuilder(args);
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
    || (apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("ApiBaseUrl must be an absolute HTTP or HTTPS URL.");
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapGet("/client-configuration", () => Results.Ok(new { ApiBaseUrl = apiUri.AbsoluteUri }));
app.MapFallbackToFile("index.html");
app.Run();
