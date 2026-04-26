using Dapper;
using Microsoft.Data.Sqlite;

namespace SQLBook.Web.Data;

public class AppDb(string connectionString)
{
    public SqliteConnection Open() => new(connectionString);

    public async Task InitialiseAsync()
    {
        using var conn = Open();
        await conn.OpenAsync();

        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS notebook_index (
                id          TEXT PRIMARY KEY,
                user_id     TEXT NOT NULL DEFAULT 'dev',
                title       TEXT NOT NULL,
                description TEXT NOT NULL DEFAULT '',
                tags        TEXT NOT NULL DEFAULT '',
                file_path   TEXT NOT NULL,
                created_at  TEXT NOT NULL,
                updated_at  TEXT NOT NULL,
                is_deleted  INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS db_connections (
                id                    TEXT PRIMARY KEY,
                user_id               TEXT NOT NULL DEFAULT 'dev',
                name                  TEXT NOT NULL,
                provider              TEXT NOT NULL,
                connection_string_enc TEXT NOT NULL
            );
        """);
    }
}
