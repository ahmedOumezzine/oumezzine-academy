using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Controllers;

public class CategoryController : Controller
{
    private readonly ICourseCatalogService _courses;

    public CategoryController(ICourseCatalogService courses)
    {
        _courses = courses;
    }

    [OutputCache(PolicyName = "PublicCatalog")]
    public async Task<IActionResult> Index()
    {
        return View(await _courses.GetCategoriesAsync());
    }

    [OutputCache(PolicyName = "PublicCatalog")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();
        var model = await _courses.GetCategoryAsync(slug);
        return model == null ? NotFound() : View(model);
    }
}