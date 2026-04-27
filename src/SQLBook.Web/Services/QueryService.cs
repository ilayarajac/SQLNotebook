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
    public async Task<CellResult> RunAsync(string? sql, Dictionary<string, string> parameters)
    {
        var result = new CellResult();
        var sw = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(sql))
        {
            result.Error = "No query to run.";
            return result;
        }

        try
        {
            using var conn = OpenConnection();
            await conn.OpenAsync();

            var dp = new DynamicParameters();
            foreach (var (k, v) in parameters)
                dp.Add(k, v);

            // Split on semicolons and execute each statement; collect every SELECT result
            var statements = sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var stmt in statements)
            {
                if (string.IsNullOrWhiteSpace(stmt)) continue;
                var rows = await conn.QueryAsync(stmt, parameters.Count > 0 ? dp : null);
                var rowList = rows.Select(r => (IDictionary<string, object?>)r).ToList();
                if (rowList.Count > 0)
                    result.ResultSets.Add(new ResultSet
                    {
                        Columns = rowList[0].Keys.ToList(),
                        Rows    = rowList.Select(r => r.Values.ToList()).ToList()
                    });
            }

            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;
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
        var provider = config["DefaultConnection:Provider"] is { Length: > 0 } p ? p : "sqlite";
        var defaultDb = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "sqlbook", "default.db");
        var connStr = config["DefaultConnection:ConnectionString"] is { Length: > 0 } cs ? cs : $"Data Source={defaultDb}";

        return provider.ToLower() switch
        {
            "sqlserver" => new SqlConnection(connStr),
            "postgres"  => new NpgsqlConnection(connStr),
            _           => new SqliteConnection(connStr)
        };
    }
}
