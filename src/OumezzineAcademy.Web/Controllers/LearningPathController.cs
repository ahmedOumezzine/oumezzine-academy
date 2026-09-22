using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Controllers;

public class LearningPathController : Controller
{
    private readonly ILearningPathCatalogService _paths;

    public LearningPathController(ILearningPathCatalogService paths)
    {
        _paths = paths;
    }

    [OutputCache(PolicyName = "PublicCatalog")]
    public async Task<IActionResult> Index(string? category, StudyLevel? level)
    {
        return View(await _paths.GetPathsAsync(category, level));
    }

    [OutputCache(PolicyName = "PublicCatalog")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();
        var model = await _paths.GetPathAsync(slug);
        return model == null ? NotFound() : View(model);
    }
}