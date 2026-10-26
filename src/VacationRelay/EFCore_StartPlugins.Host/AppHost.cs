using AspireResourceExtensionsAspire;
using Blazor.Extension;
using JavaScriptExtensionsAspire;
using PortExtensionAspire;


var builder = DistributedApplication.CreateBuilder(args);
var aspire = builder.AddAspireResource();

var api = builder.AddProject<Projects.VR_WebAPI>("api");

builder
    .AddWebAssemblyProject<Projects.EFCore_StartPlugins>("pluginGui", api)

    //.AddCommandsToModifyEnvName(new Projects.EFCore_StartPlugins(), "test")
    ;

builder
    .AddWebAssemblyProject<Projects.VR_GUI>("gui", api)
    ;

var jsTest = builder
    .AddJavaScriptApp("jsTestUIPlugins", "../VR.TestUIPlugins")
     .AddNpmCommandsFromPackage()
     //.WithPortReference(ports)
     .WaitFor(aspire!)
     ;

aspire!.Resource.AddEnvironmentVariablesTo(jsTest);

var app = builder.Build();

var result = aspire.Resource.StartParsing(app, builder);
await Task.WhenAll(app.RunAsync(), result);
