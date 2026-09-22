using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Controllers;

public class CourseController : Controller
{
    private readonly ICourseCatalogService _courses;

    public CourseController(ICourseCatalogService courses)
    {
        _courses = courses;
    }

    [OutputCache(PolicyName = "PublicCatalog")]
    public async Task<IActionResult> Index(string? q, string? category, StudyLevel? level, string? sort, string? view, int page = 1, int pageSize = 9)
    {
        var model = await _courses.SearchCoursesAsync(q, category, level, sort, view, page, pageSize);
        return View(model);
    }

    [OutputCache(PolicyName = "PublicCatalog")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();
        var model = await _courses.GetCourseAsync(slug);
        return model == null ? NotFound() : View(model);
    }
}