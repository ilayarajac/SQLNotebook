using System.Text.Json;
using Dapper;
using SQLBook.Web.Data;
using SQLBook.Web.Models;

namespace SQLBook.Web.Services;

public class NotebookService(AppDb appDb, IConfiguration config)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private string NotebooksDir()
    {
        var dir = config["Notebooks:Directory"] is { Length: > 0 } d
            ? d
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "sqlbook", "notebooks");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private string NotebookPath(string id) => Path.Combine(NotebooksDir(), $"{id}.json");

    public async Task<Notebook> CreateAsync(string title = "Untitled Notebook")
    {
        var id = "nb_" + Guid.NewGuid().ToString("N");
        var notebook = new Notebook
        {
            Id = id,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Cells =
            [
                new Cell { Id = "cell_01", Type = "sql", Order = 1, Content = "" }
            ]
        };

        await SaveFileAsync(notebook);
        await IndexAsync(notebook);
        return notebook;
    }

    public async Task<Notebook?> GetAsync(string id)
    {
        var path = NotebookPath(id);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<Notebook>(json);
    }

    public async Task SaveAsync(Notebook notebook)
    {
        notebook.UpdatedAt = DateTime.UtcNow;
        await SaveFileAsync(notebook);
        await IndexAsync(notebook);
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = appDb.Open();
        await conn.ExecuteAsync(
            "UPDATE notebook_index SET is_deleted = 1 WHERE id = @id", new { id });
    }

    public async Task<List<NotebookIndex>> ListAsync(string? search = null, string? tag = null)
    {
        using var conn = appDb.Open();
        var sql = """
            SELECT id, user_id AS UserId, title, description, tags, file_path AS FilePath,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM notebook_index
            WHERE is_deleted = 0
        """;

        if (!string.IsNullOrWhiteSpace(search))
            sql += " AND (title LIKE @search OR description LIKE @search)";
        if (!string.IsNullOrWhiteSpace(tag))
            sql += " AND tags LIKE @tag";

        sql += " ORDER BY updated_at DESC";

        return (await conn.QueryAsync<NotebookIndex>(sql, new
        {
            search = $"%{search}%",
            tag = $"%{tag}%"
        })).ToList();
    }

    private async Task SaveFileAsync(Notebook notebook)
    {
        var json = JsonSerializer.Serialize(notebook, JsonOpts);
        await File.WriteAllTextAsync(NotebookPath(notebook.Id), json);
    }

    private async Task IndexAsync(Notebook notebook)
    {
        using var conn = appDb.Open();
        await conn.ExecuteAsync("""
            INSERT INTO notebook_index (id, title, description, tags, file_path, created_at, updated_at, is_deleted)
            VALUES (@Id, @Title, @Description, @Tags, @FilePath, @CreatedAt, @UpdatedAt, 0)
            ON CONFLICT(id) DO UPDATE SET
                title = excluded.title,
                description = excluded.description,
                tags = excluded.tags,
                updated_at = excluded.updated_at
            """,
            new
            {
                notebook.Id,
                notebook.Title,
                notebook.Description,
                notebook.Tags,
                FilePath = NotebookPath(notebook.Id),
                CreatedAt = notebook.CreatedAt.ToString("o"),
                UpdatedAt = notebook.UpdatedAt.ToString("o")
            });
    }
}
