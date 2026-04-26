using Microsoft.AspNetCore.Mvc;
using SQLBook.Web.Models;
using SQLBook.Web.Services;

namespace SQLBook.Web.Controllers;

public class NotebookController(NotebookService notebooks) : Controller
{
    [HttpGet("/notebook/{id}")]
    public async Task<IActionResult> Index(string id)
    {
        var notebook = await notebooks.GetAsync(id);
        if (notebook is null) return NotFound();
        return View(notebook);
    }

    [HttpPost("/notebook/create")]
    public async Task<IActionResult> Create()
    {
        var notebook = await notebooks.CreateAsync();
        return RedirectToAction(nameof(Index), new { id = notebook.Id });
    }

    [HttpPost("/notebook/{id}/cell/add")]
    public async Task<IActionResult> AddCell(string id, string type = "sql", string? afterCellId = null)
    {
        var notebook = await notebooks.GetAsync(id);
        if (notebook is null) return NotFound();

        var newOrder = afterCellId is not null
            ? (notebook.Cells.FirstOrDefault(c => c.Id == afterCellId)?.Order ?? notebook.Cells.Count) + 1
            : notebook.Cells.Count + 1;

        // Shift orders for cells after insertion point
        foreach (var c in notebook.Cells.Where(c => c.Order >= newOrder))
            c.Order++;

        var cellNum = notebook.Cells.Count + 1;
        var cell = new Cell
        {
            Id = $"cell_{cellNum:D2}_{Guid.NewGuid().ToString("N")[..8]}",
            Type = type,
            Order = newOrder,
            Content = ""
        };

        notebook.Cells.Add(cell);
        notebook.Cells = [.. notebook.Cells.OrderBy(c => c.Order)];
        await notebooks.SaveAsync(notebook);

        return PartialView("_Cell", (notebook, cell));
    }

    [HttpDelete("/notebook/{id}/cell/{cellId}")]
    public async Task<IActionResult> DeleteCell(string id, string cellId)
    {
        var notebook = await notebooks.GetAsync(id);
        if (notebook is null) return NotFound();

        notebook.Cells.RemoveAll(c => c.Id == cellId);
        await notebooks.SaveAsync(notebook);
        return Ok();
    }

    [HttpPost("/notebook/{id}/cell/reorder")]
    public async Task<IActionResult> Reorder(string id, [FromBody] List<string> orderedIds)
    {
        var notebook = await notebooks.GetAsync(id);
        if (notebook is null) return NotFound();

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var cell = notebook.Cells.FirstOrDefault(c => c.Id == orderedIds[i]);
            if (cell is not null) cell.Order = i + 1;
        }

        notebook.Cells = [.. notebook.Cells.OrderBy(c => c.Order)];
        await notebooks.SaveAsync(notebook);
        return Ok();
    }

    [HttpPost("/notebook/{id}/autosave")]
    public async Task<IActionResult> Autosave(string id, [FromForm] string title, [FromForm] List<CellFormData> cells)
    {
        var notebook = await notebooks.GetAsync(id);
        if (notebook is null) return NotFound();

        notebook.Title = title;
        foreach (var fd in cells)
        {
            var cell = notebook.Cells.FirstOrDefault(c => c.Id == fd.Id);
            if (cell is not null) cell.Content = fd.Content;
        }

        await notebooks.SaveAsync(notebook);
        return Ok();
    }
}

public record CellFormData(string Id, string Content);
