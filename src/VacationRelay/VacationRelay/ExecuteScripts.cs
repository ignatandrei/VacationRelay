
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SqlExtensionsAspire;
using System.Data.Common;

namespace VacationRelay;

static class ExecuteScripts
{
    private static async Task<bool> ExecuteDbScriptsMongoDb(MongoDBDatabaseResource dbRes, ResourceReadyEvent ev, CancellationToken ct, string[] sqlScripts)
    {
        if (!(ev.Services.GetService(typeof(ResourceLoggerService)) is ResourceLoggerService resourceLoggerService))
        {
            Console.WriteLine("No ResourceLoggerService");
            return false;
        }
        ILogger logger = resourceLoggerService.GetLogger(dbRes);
        if (logger == null)
        {
            Console.WriteLine("No logger for " + dbRes.Name);
            return false;
        }
        return await ExecuteDbScriptsMongoDb(dbRes, logger, ct, sqlScripts);
    }
    private static async Task<bool> ExecuteDbScriptsMongoDb(MongoDBDatabaseResource dbRes, ILogger logger, CancellationToken ct,string[] sqlScripts)
    {
        
        string? cn = await dbRes.ConnectionStringExpression.GetValueAsync(ct);
        if (cn == null)
        {
            logger.LogError($"Connection string is null for resource {dbRes.Name}");
            return false;
        }
        using var connection = new MongoClient(cn);
        var res = connection.GetDatabase(dbRes.DatabaseName);

        bool hasError = false;
        foreach (var sqlScript in sqlScripts)
        {
            try
            {
                var doc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(sqlScript);
                var cmd = new BsonDocumentCommand<BsonDocument>(doc);
                var result = await res.RunCommandAsync(cmd);
                logger.LogInformation($"Executed MongoDB script: {sqlScript}, Result: {result.ToJson()}");
            }
            catch (Exception ex)
            {
                hasError = true;
                logger.LogError(ex, $"Error executing MongoDB script: {sqlScript}");
            }
        }
        if (hasError)
        {
            logger.LogError($"One or more scripts failed to execute for resource {dbRes.Name}");
            return false;
        }
        return true;
    }
    public static IResourceBuilder<MongoDBDatabaseResource> ExecuteDBScripts(this IResourceBuilder<MongoDBDatabaseResource> db, params string[] sqlScripts)
    {

        db.WithCommand("ExecuteDBScripts", "ExecuteDBScripts",async (ecc) =>
        {
            
            var result = await ExecuteDbScriptsMongoDb(db.Resource, ecc.Logger, ecc.CancellationToken, sqlScripts);
            return result ? 
            new ExecuteCommandResult() { Success = true , Message = "All scripts executed successfully." } : 
                    new ExecuteCommandResult() { Success = false, Message = "One or more scripts failed to execute." };
        });

        db.OnResourceReady(async delegate (MongoDBDatabaseResource dbRes, ResourceReadyEvent ev, CancellationToken ct)
        {
            await ExecuteDbScriptsMongoDb(dbRes, ev, ct, sqlScripts);

        });
        return db;
    }
    private static async Task<bool> ExecuteDbScriptsPostgres(PostgresDatabaseResource dbRes, ResourceReadyEvent ev, CancellationToken ct, string[] sqlScripts)
    {
        if (!(ev.Services.GetService(typeof(ResourceLoggerService)) is ResourceLoggerService resourceLoggerService))
        {
            Console.WriteLine("No ResourceLoggerService");
            return false;
        }
        ILogger logger = resourceLoggerService.GetLogger(dbRes);
        if (logger == null)
        {
            Console.WriteLine("No logger for " + dbRes.Name);
            return false;
        }
        string? cn = await dbRes.ConnectionStringExpression.GetValueAsync(ct);
        if (cn == null)
        {
            logger.LogError($"Connection string is null for resource {dbRes.Name}");
            return false;
        }
        using var connectionToRel = new Npgsql.NpgsqlConnection(cn);
        await connectionToRel.OpenAsync();
        return await ExecuteSqlScripts(connectionToRel, logger, ct, sqlScripts);
    }
    public static IResourceBuilder<PostgresDatabaseResource> ExecuteDBScripts(this IResourceBuilder<PostgresDatabaseResource> db, params string[] sqlScripts)
    {
        db.WithCommand("ExecuteDBScripts", "ExecuteDBScripts", async (ecc) =>
        {
            string? cn = await db.Resource.ConnectionStringExpression.GetValueAsync(ecc.CancellationToken);

            using var connectionToRel = new Npgsql.NpgsqlConnection(cn);
            await connectionToRel.OpenAsync();

            var result = await ExecuteSqlScripts(connectionToRel, ecc.Logger, ecc.CancellationToken, sqlScripts);
            return result ? 
                new ExecuteCommandResult() { Success = true , Message = "All scripts executed successfully." } : 
                new ExecuteCommandResult() { Success = false, Message = "One or more scripts failed to execute." };
        });
        db.OnResourceReady(async delegate (PostgresDatabaseResource dbRes, ResourceReadyEvent ev, CancellationToken ct)
        {
            await ExecuteDbScriptsPostgres(dbRes, ev, ct, sqlScripts);

        });
            return db;
    }
    
    private static async Task<bool> ExecuteSqlScripts(DbConnection con, ILogger logger, CancellationToken ct, string[] sqlScripts)         
    {
        
        if (ct.IsCancellationRequested)
        {
            return false;

        }
        bool hasError = false;
        foreach (string script in sqlScripts)
        {
            try
            {
                using var cmd = con.CreateCommand();
                cmd.CommandText = script;
                var nr = await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error {ex.Message} executing SQL script: {script} ");
                hasError = true;
            }

        }
        if (hasError)
        {
            logger.LogError($"One or more scripts failed to execute ");
            return false;
        }

        return true;
    }
}
