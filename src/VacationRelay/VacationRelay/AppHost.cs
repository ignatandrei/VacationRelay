using SqlExtensionsAspire;

var builder = DistributedApplication.CreateBuilder(args);
var aspire = builder.AddAspireResource();
var paramPass = builder.AddParameter("password", "myPa!ssW0rd");
var ports = builder.AddPort()
    .WithDeterministicPortEnvironment("postgres","sqliteweb", "mongodb", "FileDisplay")
    .WithDeterministicPortEnvironment("sqlserver",13456)
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

aspire!.Resource.AddEnvironmentVariablesTo(jsTest);


var sqlserver = builder.AddSqlServer("sqlserver", paramPass, 1433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDbGate()
    .WithAdminer(c =>
    {
        //c.WithHttpEndpoint(port,8080);
        c.WithHostPort(ports.Resource.GetDeterministicPort("sqlserver"));
        //c.WithHttpsEndpoint(13456,8080);

    })
    ;

var dbSqlServer = sqlserver.AddDatabase("vacationrelaySqlServer", "vacationrelay")
        .WithSqlPadViewerForDB(sqlserver)
        .ExecuteSqlServerScriptsAtStartup(ScriptsData.GetScripts(DatabaseType.SqlServer))
;

var postgres = builder.AddPostgres("postgres", username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb(c =>
    {
        c.WithHostPort(ports.Resource.GetDeterministicPort("postgres"));
    })
    .WithPgAdmin()
;
var postgresdb = postgres
    .AddDatabase("postgresdb", "vacationrelay")
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Postgres))
    ;

var mongo = builder.AddMongoDB("mongo", userName: username, password: password,port:5432)
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


var files = builder.CreateFileDisplay(port: ports.Resource.GetDeterministicPort("FileDisplay"));

files.AddFile(relativePath: "AppHost.cs",name:"sqlite", lines: ["builder.AddSqlite", ".ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Sqlite));"]);
files.AddFile(@"..\VR.Scripts\SQLite01.createTables.gen.txt", lines: ["CREATE TABLE vr_data", "CREATE TABLE vr_data_history"]);
files.AddFile(@"..\VR.TestUI\tests\sqlite-sqliteweb.spec.ts", lines: ["test('SQLite database is accessible and vr_data table exists in SqliteWeb'"]);

files.AddFile(relativePath: "AppHost.cs", name: "mongodb", lines: ["builder.AddMongoDB", "ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.MongoDB))"]);
files.AddFile(@"..\VR.Scripts\MongoDb01.createTables.gen.txt", lines: ["vr_data"]);
files.AddFile(@"..\VR.TestUI\tests\mongodb-mongo-express.spec.ts", lines: ["test('MongoDB vacationrelay database is visible in Mongo Express"]);


files.AddFile(relativePath: "AppHost.cs", name: "sqlserver", lines: ["builder.AddSqlServer", "ExecuteSqlServerScriptsAtStartup(ScriptsData.GetScripts(DatabaseType.SqlServer))"]);
files.AddFile(@"..\VR.Scripts\SqlServer01.createTables.gen.txt", lines: ["vr_data"]);
files.AddFile(@"..\VR.TestUI\tests\sqlserver-adminer.spec.ts", lines: ["test('SQL Server vacationrelay database is visible and tables exist in Adminer"]);

files.AddFile(relativePath: "AppHost.cs", name: "postgres", lines: ["builder.AddPostgres", "ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Postgres))"]);
files.AddFile(@"..\VR.Scripts\PostgreSQL01.createTables.gen.txt", lines: ["vr_data"]);
files.AddFile(@"..\VR.TestUI\tests\postgres-pgweb.spec.ts", lines: ["test('PostgreSQL vacationrelay database is listed and tables exist in PgWeb'"]);


var app = builder.Build();

var result = aspire.Resource.StartParsing(app,builder);
await Task.WhenAll(app.RunAsync(), result);