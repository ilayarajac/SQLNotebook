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
}
