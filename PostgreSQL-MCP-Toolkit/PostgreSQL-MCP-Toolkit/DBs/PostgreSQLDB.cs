using Npgsql;

public sealed class PostgreSQLDB
{
    private readonly string _connectionString;

    private int timeout = 0;

    public int TimeOut
    {
        get { return timeout; }
        set { timeout = value; }
    }


    public PostgreSQLDB(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public PostgreSQLDB(NpgsqlConnectionStringBuilder connectionStringBuilder, int commandTimeout)
    {
        _connectionString = connectionStringBuilder.ConnectionString;
        timeout = commandTimeout;
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}