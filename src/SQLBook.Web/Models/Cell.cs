namespace SQLBook.Web.Models;

public class Cell
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "sql"; // sql | markdown | params
    public int Order { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool Collapsed { get; set; }
    public CellResult? LastResult { get; set; }
}
