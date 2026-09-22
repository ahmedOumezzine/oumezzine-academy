using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Controllers;

public class LessonController : Controller
{
    private readonly ICourseCatalogService _courses;

    public LessonController(ICourseCatalogService courses)
    {
        _courses = courses;
    }

    [OutputCache(PolicyName = "PublicCatalog")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();
        var model = await _courses.GetLessonAsync(slug);
        return model == null ? NotFound() : View(model);
    }
}