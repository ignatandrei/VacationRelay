var builder = DistributedApplication.CreateBuilder(args);
var aspire = builder.AddAspireResource();
var paramPass = builder.AddParameter("password", "myPa!ssW0rd");
var ports = builder.AddPort()
    .WithDeterministicPortEnvironment("sqliteweb", "mongodb", "FileDisplay")
    .Construct();
var username = builder.AddParameter("username","sa");
var password = paramPass;
var portSqliteWeb = ports.Resource.GetDeterministicPort("sqliteweb");
string name1= "sqlite";
string sqliteWeb=$"{name1}-sqliteweb";

builder.AddSqlite(name1)
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

var files = builder.CreateFileDisplay(port: ports.Resource.GetDeterministicPort("FileDisplay"));
files.AddFile(relativePath: "AppHost.cs", lines: ["builder.AddSqlite", ".ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Sqlite));"]);
files.AddFile(@"..\VR.Scripts\SQLite01.createTables.gen.txt", lines: ["CREATE TABLE vr_data", "CREATE TABLE vr_data_history"]);
files.AddFile(@"..\VR.TestUI\tests\sqlite-sqliteweb.spec.ts",["test('SQLite database is accessible and vr_data table exists in SqliteWeb'"]);
aspire!.Resource.AddEnvironmentVariablesTo(jsTest);


//var sqlserver = builder.AddSqlServer("sqlserver", paramPass, 1433)
//    .WithLifetime(ContainerLifetime.Persistent)
//    .WithDbGate()
//    .WithAdminer()

//;

//var dbSqlServer = sqlserver.AddDatabase("vacationrelaySqlServer", "vacationrelay")
//        .WithSqlPadViewerForDB(sqlserver)
//        .ExecuteSqlServerScriptsAtStartup(ScriptsData.GetScripts(DatabaseType.SqlServer))
//;
//var postgres = builder.AddPostgres("postgres",username,password)
//    .WithLifetime(ContainerLifetime.Persistent)
//    .WithPgWeb()
//    .WithPgAdmin()
//;
//var postgresdb = postgres
//    .AddDatabase("postgresdb","vacationrelay")
//    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Postgres))
//    ;

var mongo = builder.AddMongoDB("mongo", userName: username, password: password)
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithMongoExpress(c=>
                   {
                       c.WithHostPort(ports.Resource.GetDeterministicPort("mongodb"));
                   })
                   //.WithDbGate()
;
var mongodb = mongo
    .AddDatabase("mongodb", "vacationrelay")
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.MongoDB))
    ;

var app = builder.Build();

var result = aspire.Resource.StartParsing(app,builder);
await Task.WhenAll(app.RunAsync(), result);