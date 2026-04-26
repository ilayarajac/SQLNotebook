using Microsoft.AspNetCore.Mvc;
using SQLBook.Web.Services;

namespace SQLBook.Web.Controllers;

public class LibraryController(NotebookService notebooks) : Controller
{
    [HttpGet("/")]
    [HttpGet("/library")]
    public async Task<IActionResult> Index(string? search, string? tag)
    {
        var list = await notebooks.ListAsync(search, tag);
        ViewBag.Search = search;
        ViewBag.Tag = tag;
        return View(list);
    }
}
