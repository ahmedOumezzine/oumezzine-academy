using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class LearningPathsController : Controller
{
    private readonly AdminLearningPathService _service;

    public LearningPathsController(AdminLearningPathService service) => _service = service;

    public async Task<IActionResult> Index(CancellationToken token) => View(await _service.ListAsync(token));

    [HttpGet] public async Task<IActionResult> Create(CancellationToken token) => View("Edit", await _service.GetFormAsync(null, token));

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken token)
    {
        var model = await _service.GetFormAsync(id, token);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Create(LearningPathEditViewModel model, CancellationToken token) => Save(model, token);

    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Edit(LearningPathEditViewModel model, CancellationToken token) => Save(model, token);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        try { await _service.DeleteAsync(id, token); TempData["Success"] = "Parcours supprimé.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message;
    }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("admin/learningpaths/{id:guid}/courses")]
    public async Task<IActionResult> Courses(Guid id, CancellationToken token)
    {
        var model = await _service.CoursesAsync(id, token);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("admin/learningpaths/{id:guid}/courses/add"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCourse(Guid id, Guid courseId, CancellationToken token)
    {
        try { await _service.AddCourseAsync(id, courseId, token); TempData["Success"] = "Cours ajouté au parcours.";
        }
        catch (InvalidOperationException) { TempData["Error"] = "Ce cours est déjà présent dans le parcours.";
    }
        catch (KeyNotFoundException) { TempData["Error"] = "Le cours ou le parcours demandé est introuvable.";
    }
        return RedirectToAction(nameof(Courses), new { id });
    }

    [HttpPost("admin/learningpaths/{id:guid}/courses/{courseLinkId:guid}/move"), ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveCourse(Guid id, Guid courseLinkId, int direction, CancellationToken token)
    {
        await _service.MoveCourseAsync(id, courseLinkId, direction, token);
        return RedirectToAction(nameof(Courses), new { id });
    }

    [HttpPost("admin/learningpaths/{id:guid}/courses/{courseLinkId:guid}/remove"), ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCourse(Guid id, Guid courseLinkId, CancellationToken token)
    {
        await _service.RemoveCourseAsync(id, courseLinkId, token);
        return RedirectToAction(nameof(Courses), new { id });
    }

    private async Task<IActionResult> Save(LearningPathEditViewModel model, CancellationToken token)
    {
        Validate(model.French, "French"); Validate(model.English, "English");
        if (model.CategoryId == Guid.Empty) ModelState.AddModelError(nameof(model.CategoryId), "Sélectionnez une catégorie.");
        if (!string.IsNullOrWhiteSpace(model.French.Slug) && await _service.SlugExistsAsync("fr", model.French.Slug, model.Id, token)) ModelState.AddModelError("French.Slug", "Ce slug existe déjà en français.");
        if (!string.IsNullOrWhiteSpace(model.English.Slug) && await _service.SlugExistsAsync("en", model.English.Slug, model.Id, token)) ModelState.AddModelError("English.Slug", "Ce slug existe déjà en anglais.");
        if (!ModelState.IsValid) {
        var prepared = await _service.GetFormAsync(model.Id == Guid.Empty ? null : model.Id, token); model.Categories = prepared?.Categories ?? []; model.SelectedCategoryTitle = prepared?.SelectedCategoryTitle ?? "Catégorie";
        return View("Edit", model);
    }
        try { await _service.SaveAsync(model, token); TempData["Success"] = "Parcours enregistré.";
        return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message);
        return View("Edit", model);
    }
    }

    private void Validate(LearningPathTranslationInput input, string key)
    {
        if (string.IsNullOrWhiteSpace(input.Title) != string.IsNullOrWhiteSpace(input.Slug)) ModelState.AddModelError(key, "Le titre et le slug doivent être renseignés ensemble.");
        if (input.PublicationStatus == StudyStatus.Published && (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Slug) || string.IsNullOrWhiteSpace(input.Summary))) ModelState.AddModelError(key, $"La traduction {key} publiée doit avoir un titre, un slug et un résumé.");
    }
}


