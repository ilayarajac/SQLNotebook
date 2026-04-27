namespace SQLBook.Web.Models;

public class ResultSet
{
    public List<string> Columns { get; set; } = [];
    public List<List<object?>> Rows { get; set; } = [];
    public int RowCount => Rows.Count;
}

public class CellResult
{
    public List<ResultSet> ResultSets { get; set; } = [];
    public long ElapsedMs { get; set; }
    public int RowCount => ResultSets.Sum(r => r.RowCount);
    public string? Error { get; set; }
    public bool HasError => Error is not null;
}
