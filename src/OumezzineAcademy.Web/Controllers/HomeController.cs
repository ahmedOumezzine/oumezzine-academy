using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Controllers;

public class HomeController : Controller
{
    private readonly ICourseCatalogService _courses;

    public HomeController(ICourseCatalogService courses)
    {
        _courses = courses;
    }

    [OutputCache(PolicyName = "PublicCatalog")]
    public async Task<IActionResult> Index()
    {
        var model = await _courses.GetHomeAsync();
        return View(model);
    }

    public IActionResult About()
    {
        return View();
    }
}