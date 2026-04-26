namespace SQLBook.Web.Models;

public class Notebook
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = "Untitled Notebook";
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string ConnectionHint { get; set; } = string.Empty;
    public List<Cell> Cells { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
