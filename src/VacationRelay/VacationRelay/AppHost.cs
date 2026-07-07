using SqlExtensionsAspire;
using VR.Scripts;
var builder = DistributedApplication.CreateBuilder(args);

var paramPass = builder.AddParameter("password", "myP@ssW0rd");

var sqlserver = builder.AddSqlServer("sqlserver", paramPass, 1433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDbGate()
    .WithAdminer()

;

var db = sqlserver.AddDatabase("vacationrelay")
        .WithSqlPadViewerForDB(sqlserver)
        .ExecuteSqlServerScriptsAtStartup(ScriptsData.GetScripts(DatabaseType.SqlServer))
;

builder.Build().Run();
