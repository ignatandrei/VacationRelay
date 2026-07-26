using AspireResourceExtensionsAspire;
using JavaScriptExtensionsAspire;
using SqlExtensionsAspire;
using VacationRelay;
using VR.Scripts;
var builder = DistributedApplication.CreateBuilder(args);

var aspire = builder.AddAspireResource();
ArgumentNullException.ThrowIfNull(aspire, "Aspire resource is not available");
var paramPass = builder.AddParameter("password", "myPa!ssW0rd");

var sqlserver = builder.AddSqlServer("sqlserver", paramPass, 1433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDbGate()
    .WithAdminer()

;

var dbSqlServer = sqlserver.AddDatabase("vacationrelaySqlServer", "vacationrelay")
        .WithSqlPadViewerForDB(sqlserver)
        .ExecuteSqlServerScriptsAtStartup(ScriptsData.GetScripts(DatabaseType.SqlServer))
;

var username = builder.AddParameter("username","sa");
var password = paramPass;
var postgres = builder.AddPostgres("postgres",username,password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb()
    .WithPgAdmin()
;
var postgresdb = postgres
    .AddDatabase("postgresdb","vacationrelay")
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Postgres))
    ;

var mongo = builder.AddMongoDB("mongo",userName:username,password:password)
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithMongoExpress()
                    .WithDbGate()
;
var mongodb = mongo
    .AddDatabase("mongodb","vacationrelay")
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.MongoDB))
    ;

var tests = builder.AddJavaScriptApp("tests", @"..\VR.TestUI")
        .AddNpmCommandsFromPackage()
        ;

aspire.Resource.AddEnvironmentVariablesTo(tests);

var app = builder.Build();
var result = aspire.Resource.StartParsing(app, builder);

var runTask = app.RunAsync();

await Task.WhenAll(runTask,result);
//aspire.Resource.AddEnvironmentVariablesTo(tests);
//await runTask;


