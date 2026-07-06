using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

public sealed class DocsController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public DocsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("/docs/team-guide")]
    public IActionResult TeamGuide()
    {
        return MarkdownDocument("TEAM_GUIDE.md");
    }

    [HttpGet("/docs/development-spec")]
    public IActionResult DevelopmentSpec()
    {
        return MarkdownDocument("DEVELOPMENT_SPEC.md");
    }

    private IActionResult MarkdownDocument(string fileName)
    {
        var docsPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "docs", fileName));
        if (!System.IO.File.Exists(docsPath))
        {
            return NotFound();
        }

        return PhysicalFile(docsPath, "text/markdown; charset=utf-8");
    }
}
