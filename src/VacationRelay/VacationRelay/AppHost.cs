using AspireResourceExtensionsAspire;
using JavaScriptExtensionsAspire;
using PortExtensionAspire;
using VacationRelay;
using VR.Scripts;
var builder = DistributedApplication.CreateBuilder(args);

var aspire = builder.AddAspireResource();

var paramPass = builder.AddParameter("password", "myPa!ssW0rd");
var ports = builder.AddPort()
    .WithDeterministicPortEnvironment("sqliteweb", "mongodb")
    .Construct();

//var sqlserver = builder.AddSqlServer("sqlserver", paramPass, 1433)
//    .WithLifetime(ContainerLifetime.Persistent)
//    .WithDbGate()
//    .WithAdminer()

//;

//var dbSqlServer = sqlserver.AddDatabase("vacationrelaySqlServer", "vacationrelay")
//        .WithSqlPadViewerForDB(sqlserver)
//        .ExecuteSqlServerScriptsAtStartup(ScriptsData.GetScripts(DatabaseType.SqlServer))
//;

var username = builder.AddParameter("username","sa");
var password = paramPass;
//var postgres = builder.AddPostgres("postgres",username,password)
//    .WithLifetime(ContainerLifetime.Persistent)
//    .WithPgWeb()
//    .WithPgAdmin()
//;
//var postgresdb = postgres
//    .AddDatabase("postgresdb","vacationrelay")
//    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Postgres))
//    ;

var mongo = builder.AddMongoDB("mongo", userName: username, password: password,port: ports.Resource.GetDeterministicPort("mongodb"))
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithMongoExpress()
                    .WithPortReference(ports)
                    .WithDbGate()
;
var mongodb = mongo
    .AddDatabase("mongodb", "vacationrelay")
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.MongoDB))
    ;

var portSqliteWeb= ports.Resource.GetDeterministicPort("sqliteweb");

string name1= "sqlite";
string sqliteWeb=$"{name1}-sqliteweb";
builder.AddSqlite(name1)
    //.WithPortReference(ports)
    .WithSqliteWeb(c =>
    {
        c.WithHttpEndpoint(targetPort: 8080, name: "http", port: portSqliteWeb);
       
    })
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Sqlite));


var jsTest = builder
    .AddJavaScriptApp("jsTest", "../VR.TestUI")    
     .AddNpmCommandsFromPackage()
     .WithPortReference(ports)
     .WaitFor(aspire!)
     ;

;
aspire.Resource.AddEnvironmentVariablesTo(jsTest);
//builder.Build().Run();
var  app = builder.Build();

var result = aspire.Resource.StartParsing(app,builder);
await Task.WhenAll(app.RunAsync(), result);