
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SqlExtensionsAspire;
using System.Data.Common;

namespace VacationRelay;

static class ExecuteScripts
{
    public static IResourceBuilder<T> ExecuteDBScripts<T>(this IResourceBuilder<T> db, params string[] sqlScripts) 
        where T : IResourceWithConnectionString
    {
        
        db.OnResourceReady(async delegate (T dbRes, ResourceReadyEvent ev, CancellationToken ct)
        {
            string? cn = await dbRes.ConnectionStringExpression.GetValueAsync(ct);
            if (cn != null)
            {
                
                if (!(ev.Services.GetService(typeof(ResourceLoggerService)) is ResourceLoggerService resourceLoggerService))
                {
                    Console.WriteLine("No ResourceLoggerService");
                }
                else
                {
                    ILogger logger = resourceLoggerService.GetLogger(db.Resource);
                    if (logger == null)
                    {
                        Console.WriteLine("No logger for " + db.Resource.Name);
                    }
                    else
                    {
                        var type = typeof(T);
                        DbConnection? connectionToRel = null;

                    if (type == typeof(MongoDBDatabaseResource))
                    {
                            var connection = new MongoClient(cn);
                            var doc =MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>("{ create: \"teastcollection\"}");
                            var cmd = new BsonDocumentCommand<BsonDocument>(doc);
                            //var cmd = new JsonCommand<BsonDocument>(" { create: \"teastcollection\", capped: true, size: 64 * 1024 }");
                            //Dictionary<string,object> create=new Dictionary<string, object>();
                            //create.Add("create", "test23dd");
                            //create.Add("capped", true);
                            //create.Add("size", 64 * 1024);
                            //var cmd= new BsonDocumentCommand<BsonDocument>(new BsonDocument(create));
                            var res= await connection.GetDatabase("vacationrelay").RunCommandAsync(cmd);
                            logger.LogInformation($"MongoDB command result: {res.ToJson()}");
                            //await ExecuteMongoScripts(connection, logger, ct,sqlScripts);
                        }
                    if (type == typeof(PostgresDatabaseResource))
                    {
                        connectionToRel = new Npgsql.NpgsqlConnection(cn);
                        await connectionToRel.OpenAsync();
                        await ExecuteSqlScripts(connectionToRel, logger, ct,  sqlScripts);
                    }
                    if(connectionToRel == null)
                    {
                        logger.LogWarning($" {nameof(ExecuteDBScripts)} is not supported for resource type {type.Name}");
                    }


                    }
                }
            }
        });
        return db;

    }

    private static async Task<bool> ExecuteSqlScripts(DbConnection con, ILogger logger, CancellationToken ct, string[] sqlScripts)         
    {
        
        if (ct.IsCancellationRequested)
        {
            return false;

        }
        foreach (string script in sqlScripts)
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = script;
            await cmd.ExecuteNonQueryAsync();
        }

        return false;
    }
}
