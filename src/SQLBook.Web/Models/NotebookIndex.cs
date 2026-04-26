namespace SQLBook.Web.Models;

public class NotebookIndex
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = "dev";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
