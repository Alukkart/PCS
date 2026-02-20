namespace PCS.Data;

public static class DatabaseSettings
{
    public const string ConnectionStringEnvName = "PCS_CONNECTION_STRING";
    private const string DefaultConnectionString =
        "Data Source=pcs_library.db";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionStringEnvName) ?? DefaultConnectionString;
}
