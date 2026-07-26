
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SqlExtensionsAspire;
using System.Data.Common;
using Microsoft.Data.Sqlite;

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
                var result = await res.RunCommandAsync(cmd, cancellationToken: ct);
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
        await connectionToRel.OpenAsync(ct);
        return await ExecuteSqlScripts(connectionToRel, logger, ct, sqlScripts);
    }
    public static IResourceBuilder<PostgresDatabaseResource> ExecuteDBScripts(this IResourceBuilder<PostgresDatabaseResource> db, params string[] sqlScripts)
    {
        db.WithCommand("ExecuteDBScripts", "ExecuteDBScripts", async (ecc) =>
        {
            string? cn = await db.Resource.ConnectionStringExpression.GetValueAsync(ecc.CancellationToken);
            if (string.IsNullOrWhiteSpace(cn))
            {
                ecc.Logger.LogError("Connection string is null or empty.");
                return new ExecuteCommandResult() { Success = false, Message = "Connection string is null or empty." };
            }

            using var connectionToRel = new Npgsql.NpgsqlConnection(cn);
            await connectionToRel.OpenAsync(ecc.CancellationToken);

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

    private static async Task<bool> ExecuteDbScriptsSqlite(SqliteResource dbRes, ResourceReadyEvent ev, CancellationToken ct, string[] sqlScripts)
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
        if (string.IsNullOrWhiteSpace(cn))
        {
            logger.LogError($"Connection string is null or empty for resource {dbRes.Name}");
            return false;
        }

        using var connection = new SqliteConnection(cn);
        await connection.OpenAsync(ct);
        return await ExecuteSqlScripts(connection, logger, ct, sqlScripts);
    }

    public static IResourceBuilder<SqliteResource> ExecuteDBScripts(this IResourceBuilder<SqliteResource> db, params string[] sqlScripts)
    {
        db.WithCommand("ExecuteDBScripts", "ExecuteDBScripts", async (ecc) =>
        {
            string? cn = await db.Resource.ConnectionStringExpression.GetValueAsync(ecc.CancellationToken);
            if (string.IsNullOrWhiteSpace(cn))
            {
                ecc.Logger.LogError("Connection string is null or empty.");
                return new ExecuteCommandResult() { Success = false, Message = "Connection string is null or empty." };
            }

            using var connection = new SqliteConnection(cn);
            await connection.OpenAsync(ecc.CancellationToken);

            var result = await ExecuteSqlScripts(connection, ecc.Logger, ecc.CancellationToken, sqlScripts);
            return result ?
                new ExecuteCommandResult() { Success = true, Message = "All scripts executed successfully." } :
                new ExecuteCommandResult() { Success = false, Message = "One or more scripts failed to execute." };
        });

        db.OnResourceReady(async delegate (SqliteResource dbRes, ResourceReadyEvent ev, CancellationToken ct)
        {
            await ExecuteDbScriptsSqlite(dbRes, ev, ct, sqlScripts);

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
            if (ct.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                using var cmd = con.CreateCommand();
                cmd.CommandText = script;
                await cmd.ExecuteNonQueryAsync(ct);
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
