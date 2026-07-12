namespace VR.Scripts;

public class ScriptsData
{
    public static string[] GetScripts(DatabaseType databaseType)
    {
        var ret = databaseType switch
        {
            DatabaseType.None => Array.Empty<string>(),
            DatabaseType.SqlServer =>
            [
                MyAdditionalFiles.SqlServer01_createTables_gen_txt
            ],
            DatabaseType.Postgres =>
            [
                MyAdditionalFiles.PostgreSQL01_createTables_gen_txt
            ],
            DatabaseType.MongoDB =>
            [
                MyAdditionalFiles.MongoDb01_createTables_gen_txt,
                MyAdditionalFiles.MongoDb02_createTables_gen_txt

            ],
            _ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, "Unsupported database type")
        };
        return ret;
    }
}
