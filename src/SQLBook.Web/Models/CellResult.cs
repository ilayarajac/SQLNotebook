namespace SQLBook.Web.Models;

public class CellResult
{
    public List<string> Columns { get; set; } = [];
    public List<List<object?>> Rows { get; set; } = [];
    public long ElapsedMs { get; set; }
    public int RowCount => Rows.Count;
    public string? Error { get; set; }
    public bool HasError => Error is not null;
}
