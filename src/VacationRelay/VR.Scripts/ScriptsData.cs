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
            _ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType.ToString())
        };
        return ret;
}   
}
