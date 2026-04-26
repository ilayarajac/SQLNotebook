using Dapper;
using Microsoft.Data.Sqlite;

namespace SQLBook.Web.Services;

public record SchemaTable(string TableName, List<SchemaColumn> Columns);
public record SchemaColumn(string ColumnName, string DataType);

public class SchemaService(IConfiguration config)
{
    public async Task<List<SchemaTable>> GetSchemaAsync()
    {
        var provider = config["DefaultConnection:Provider"] ?? "sqlite";
        var connStr = config["DefaultConnection:ConnectionString"]
            ?? $"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "sqlbook", "default.db")}";

        try
        {
            using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();

            if (provider.ToLower() == "sqlite")
                return await GetSqliteSchemaAsync(conn);

            var tables = (await conn.QueryAsync<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' OR table_schema = 'dbo' ORDER BY table_name")).ToList();

            var schema = new List<SchemaTable>();
            foreach (var table in tables)
            {
                var cols = (await conn.QueryAsync<SchemaColumn>(
                    "SELECT column_name AS ColumnName, data_type AS DataType FROM information_schema.columns WHERE table_name = @table ORDER BY ordinal_position",
                    new { table })).ToList();
                schema.Add(new SchemaTable(table, cols));
            }
            return schema;
        }
        catch
        {
            return [];
        }
    }

    private static async Task<List<SchemaTable>> GetSqliteSchemaAsync(SqliteConnection conn)
    {
        var tables = (await conn.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")).ToList();

        var schema = new List<SchemaTable>();
        foreach (var table in tables)
        {
            var cols = (await conn.QueryAsync<dynamic>($"PRAGMA table_info(\"{table}\")")).ToList();
            var columns = cols.Select(c => new SchemaColumn(
                (string)c.name,
                (string)c.type)).ToList();
            schema.Add(new SchemaTable(table, columns));
        }
        return schema;
    }
}
