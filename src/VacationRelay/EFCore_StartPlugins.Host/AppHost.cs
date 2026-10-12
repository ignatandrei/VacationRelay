using Blazor.Extension;


var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.VR_WebAPI>("api");
builder
    .AddWebAssemblyProject<Projects.EFCore_StartPlugins>("pluginGui", api)

    //.AddCommandsToModifyEnvName(new Projects.EFCore_StartPlugins(), "test")
    ;
builder.Build().Run();
