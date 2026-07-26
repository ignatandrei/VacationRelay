The first iteration of the project should create the database – no matter if SqlServer, PostgreSql, Sqlite or MongoDB ( see references ) .

Database independence sounds simple until,because I use simple connections, the app should do schema creation. The challenge is that each backend speaks a different language. SQL Server and PostgreSQL need SQL DDL( not the same, though ), while MongoDB uses JSON commands. 

This is the code for MongoDb ( run each line separately )

```json
{ "create": "vr_data" }
{ "create": "vr_data_history" }
```

And for Sql Server

```sql
DROP TABLE IF EXISTS vr_data_history;
DROP TABLE IF EXISTS vr_data;

CREATE TABLE vr_data (
    id               TEXT NOT NULL PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    idpersoninvacation TEXT NOT NULL,
    idpersonreplace    TEXT NOT NULL,
    fromdate           TEXT NOT NULL,
    todate             TEXT NULL
);

CREATE TABLE vr_data_history (
    id               TEXT NOT NULL PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    idpersoninvacation TEXT NOT NULL,
    idpersonreplace    TEXT NOT NULL,
    fromdate           TEXT NOT NULL,
    todate             TEXT NULL
);
```


My approach was to let Aspire orchestrate the databases and run the correct script set at startup. Scripts are stored in project `VR.Scripts`, generated into C# resources with RSCG Utils, and executed through  Aspire when each resource becomes ready (`OnResourceReady`), so every provider initializes automatically. Also, each database comes with a viewer to assert that the initialization is correct

This is the ASPIRE code

```csharp
var builder = DistributedApplication.CreateBuilder(args);

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

builder.AddSqlite("sqlite")
    .WithSqliteWeb()
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Sqlite));

builder.Build().Run();

```

This keeps initialization of schema consistent while preserving engine-specific syntax.



References:  

Part 001 idea: https://msprogrammer.serviciipeweb.ro/varedain-start-idea-part-001-of-n  

RSCG Utils: https://ignatandrei.github.io/RSCG_Examples/v2/docs/RSCG_Utils

Code for scripting schema https://github.com/ignatandrei/VacationRelay/tree/2662d687a0ee570339cd7b9042642cb55ab51fbd/src/VacationRelay/VR.Scripts




