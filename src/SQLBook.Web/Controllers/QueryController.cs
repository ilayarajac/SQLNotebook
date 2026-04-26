using Microsoft.AspNetCore.Mvc;
using SQLBook.Web.Services;

namespace SQLBook.Web.Controllers;

public class QueryController(QueryService queryService) : Controller
{
    [HttpPost("/query/run")]
    public async Task<IActionResult> Run([FromForm] string sql, [FromForm] string cellId)
    {
        // Collect any @param= style parameters from form
        var parameters = Request.Form
            .Where(kv => kv.Key.StartsWith("param_"))
            .ToDictionary(kv => kv.Key[6..], kv => kv.Value.ToString());

        var result = await queryService.RunAsync(sql, parameters);
        ViewBag.CellId = cellId;
        return PartialView("~/Views/Notebook/_ResultTable.cshtml", result);
    }
}
