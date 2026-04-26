using Microsoft.AspNetCore.Mvc;
using SQLBook.Web.Services;

namespace SQLBook.Web.Controllers;

public class SchemaController(SchemaService schemaService) : Controller
{
    [HttpGet("/schema")]
    public async Task<IActionResult> Index()
    {
        var schema = await schemaService.GetSchemaAsync();
        return PartialView("_SchemaSidebar", schema);
    }

    // JSON endpoint consumed by CodeMirror autocomplete
    [HttpGet("/api/schema")]
    public async Task<IActionResult> GetJson()
    {
        var schema = await schemaService.GetSchemaAsync();
        return Json(schema);
    }
}
