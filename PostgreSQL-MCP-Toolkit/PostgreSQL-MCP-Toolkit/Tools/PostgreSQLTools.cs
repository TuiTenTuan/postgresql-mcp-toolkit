using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ModelContextProtocol.Server;
using Npgsql;
using System.ComponentModel;

[McpServerToolType]
public class PostgreSQLTools
{
    private readonly PostgreSQLDB _db;

    public PostgreSQLTools(PostgreSQLDB postgreSQLDB)
    {
        _db = postgreSQLDB;
    }

    [McpServerTool(Name = "list_tables")]
    [Description(
        "Lists base tables in the connected PostgreSQL database. " +
        "Returns schema-qualified names in the form schema.table " +
        "(for example users). System schemas pg_catalog and information_schema are excluded." +
        "Optional schema parameter (defaults to public).")]
    public async Task<string> ListTables(
        [Description("Schema name (defaults to public).")] string schema = "public",
        CancellationToken cancellationToken = default)
    {
        string sql = """
            SELECT table_schema || '.' || table_name AS table_name
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE'
              AND table_schema = @schema
            ORDER BY table_schema, table_name;
            """;

        try
        {
            var tables = new List<string>();

            await using (var conn = _db.CreateConnection())
            {
                await conn.OpenAsync(cancellationToken);
                await using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandTimeout = _db.TimeOut;

                    var schemaParam = command.CreateParameter();
                    schemaParam.ParameterName = "@schema";
                    schemaParam.Value = schema;
                    command.Parameters.Add(schemaParam);

                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return MCPUtics.Instance.SerializeOutput(tables);
        }
        catch (Exception ex)
        {
            return MCPUtics.Instance.FormatError(ex);
        }
    }

    [McpServerTool(Name = "get_table_structure")]
    [Description(
        "Returns column metadata for a PostgreSQL table: column name, data type, nullability, " +
        "default value, and whether the column is part of the primary key. " +
        "Optional schema parameter (defaults to public).")]
    public async Task<string> GetTableStructure(
        [Description("Table name as table or table (public schema if omitted).")] string tableName,
        [Description("Schema name (defaults to public).")] string schema = "public",
        CancellationToken cancellationToken = default)
    {

        const string sql = """
            SELECT
                c.column_name,
                c.data_type,
                c.udt_name,
                c.is_nullable,
                c.column_default,
                CASE
                    WHEN pk.column_name IS NOT NULL THEN TRUE
                    ELSE FALSE
                END AS is_primary_key
            FROM information_schema.columns AS c
            LEFT JOIN (
                SELECT kcu.column_name
                FROM information_schema.table_constraints AS tc
                INNER JOIN information_schema.key_column_usage AS kcu
                    ON tc.constraint_name = kcu.constraint_name
                   AND tc.table_schema = kcu.table_schema
                   AND tc.table_name = kcu.table_name
                WHERE tc.constraint_type = 'PRIMARY KEY'
                  AND tc.table_schema = @schema
                  AND tc.table_name = @table
            ) AS pk ON c.column_name = pk.column_name
            WHERE c.table_schema = @schema
              AND c.table_name = @table
            ORDER BY c.ordinal_position;
            """;

        List<object> columns = new List<object>();

        try
        {
            await using (var conn = _db.CreateConnection())
            {
                await conn.OpenAsync(cancellationToken);
                await using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandTimeout = _db.TimeOut;

                    var schemaParam = command.CreateParameter();
                    schemaParam.ParameterName = "@schema";
                    schemaParam.Value = schema;
                    command.Parameters.Add(schemaParam);

                    var tableParam = command.CreateParameter();
                    tableParam.ParameterName = "@table";
                    tableParam.Value = tableName;
                    command.Parameters.Add(tableParam);

                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            columns.Add(new
                            {
                                column_name = reader.GetString(0),
                                data_type = reader.GetString(1),
                                udt_name = reader.GetString(2),
                                is_nullable = reader.GetString(3),
                                column_default = reader.IsDBNull(4) ? null : reader.GetString(4),
                                is_primary_key = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }

            if (columns.Count == 0)
            {
                return MCPUtics.Instance.SerializeOutput(new { error = $"Table '{schema}.{tableName}' not found or has no columns." });
            }

            return MCPUtics.Instance.SerializeOutput(new { schema, tableName, columns });
        }
        catch (Exception ex)
        {
            return MCPUtics.Instance.FormatError(ex);
        }
    }

    [McpServerTool(Name = "execute_raw_sql")]
    [Description(
        "Executes a SQL statement against the configured PostgreSQL database. " +
        "Supports both read (SELECT) and write (INSERT/UPDATE/DELETE/DDL) statements. " +
        "Returns result rows as JSON when available, or rows-affected for non-query statements." +
        "This tool will execute the provided SQL statement as-is without any modification, so the statement should be carefully constructed to avoid SQL injection or other unintended effects." +
        "Also, this must include schema-qualified table names when referencing tables.")]
    public async Task<string> ExecuteRawSql(
        [Description("SQL statement to execute against the connected database.")] string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return MCPUtics.Instance.SerializeOutput(new { error = "SQL must not be empty." });
        }

        try
        {
            await using (var conn = _db.CreateConnection())
            {
                await conn.OpenAsync(cancellationToken);

                await using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandTimeout = _db.TimeOut;

                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (reader.FieldCount == 0)
                        {
                            return MCPUtics.Instance.SerializeOutput(new { message = "Statement executed successfully.", rows_affected = reader.RecordsAffected });
                        }

                        var columnNames = new string[reader.FieldCount];
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            columnNames[i] = reader.GetName(i);
                        }

                        var rows = new List<Dictionary<string, object?>>();
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var row = new Dictionary<string, object?>(reader.FieldCount);
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                row[columnNames[i]] = reader.IsDBNull(i) ? null : MCPUtics.Instance.ConvertValue(reader.GetValue(i));
                            }

                            rows.Add(row);
                        }

                        return MCPUtics.Instance.SerializeOutput(new { columns = columnNames, row_count = rows.Count, rows });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return MCPUtics.Instance.FormatError(ex);
        }
    }
}