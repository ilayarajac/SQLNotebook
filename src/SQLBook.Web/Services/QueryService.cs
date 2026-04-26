using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Npgsql;
using SQLBook.Web.Models;

namespace SQLBook.Web.Services;

public class QueryService(IConfiguration config)
{
    public async Task<CellResult> RunAsync(string sql, Dictionary<string, string> parameters)
    {
        var result = new CellResult();
        var sw = Stopwatch.StartNew();

        try
        {
            using var conn = OpenConnection();
            await conn.OpenAsync();

            var dp = new DynamicParameters();
            foreach (var (k, v) in parameters)
                dp.Add(k, v);

            // Execute all statements, return last SELECT result
            var statements = sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            IEnumerable<IDictionary<string, object?>>? lastRows = null;

            foreach (var stmt in statements)
            {
                if (string.IsNullOrWhiteSpace(stmt)) continue;
                lastRows = await conn.QueryAsync<IDictionary<string, object?>>(stmt, dp) as IEnumerable<IDictionary<string, object?>>;
            }

            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;

            if (lastRows is not null)
            {
                var rowList = lastRows.ToList();
                if (rowList.Count > 0)
                {
                    result.Columns = rowList[0].Keys.ToList();
                    result.Rows = rowList.Select(r => r.Values.Cast<object?>().ToList()).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;
            result.Error = ex.Message;
        }

        return result;
    }

    private DbConnection OpenConnection()
    {
        var provider = config["DefaultConnection:Provider"] ?? "sqlite";
        var connStr = config["DefaultConnection:ConnectionString"]
            ?? $"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "sqlbook", "default.db")}";

        return provider.ToLower() switch
        {
            "sqlserver" => new SqlConnection(connStr),
            "postgres"  => new NpgsqlConnection(connStr),
            _           => new SqliteConnection(connStr)
        };
    }
}
