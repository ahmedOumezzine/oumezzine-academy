using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class LearningPathCategoriesController : Controller
{
    private readonly AdminLearningPathCategoryService _service;

    public LearningPathCategoriesController(AdminLearningPathCategoryService service) => _service = service;

    public async Task<IActionResult> Index(CancellationToken token) => View(await _service.ListAsync(token));

    [HttpGet] public IActionResult Create() => View(new LearningPathCategoryEditViewModel());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken token)
    {
        var e = await _service.FindAsync(id, token);
        return e is null ? NotFound() : View("Create", new LearningPathCategoryEditViewModel { Id = e.Id, French = Input(e.Translations.FirstOrDefault(x => x.LanguageCode == "fr")), English = Input(e.Translations.FirstOrDefault(x => x.LanguageCode == "en")) });
    }

    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Create(LearningPathCategoryEditViewModel model, CancellationToken token) => Save(model, token);

    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Edit(LearningPathCategoryEditViewModel model, CancellationToken token) => Save(model, token);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        try { await _service.DeleteAsync(id, token); TempData["Success"] = "Catégorie supprimée.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { TempData["Error"] = ex.Message;
    }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(LearningPathCategoryEditViewModel model, CancellationToken token)
    { Validate(model.French, "French"); Validate(model.English, "English");
        if (!string.IsNullOrWhiteSpace(model.French.Slug) && await _service.SlugExistsAsync("fr", model.French.Slug, model.Id, token)) ModelState.AddModelError("French.Slug", "Ce slug existe déjà en français.");
        if (!string.IsNullOrWhiteSpace(model.English.Slug) && await _service.SlugExistsAsync("en", model.English.Slug, model.Id, token)) ModelState.AddModelError("English.Slug", "Ce slug existe déjà en anglais.");
        if (!ModelState.IsValid) return View("Create", model);
        try { await _service.SaveAsync(model, token);
        return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message);
        return View("Create", model);
    } }

    private static LearningPathCategoryTranslationInput Input(AdminLearningPathCategoryTranslationDto? x) => x is null ? new() : new() { Title = x.Title, Slug = x.Slug, Summary = x.Summary, MetaTitle = x.MetaTitle, MetaDescription = x.MetaDescription, PublicationStatus = x.PublicationStatus };

    private void Validate(LearningPathCategoryTranslationInput input, string key)
    {
        if (string.IsNullOrWhiteSpace(input.Title) != string.IsNullOrWhiteSpace(input.Slug)) ModelState.AddModelError(key, "Le titre et le slug doivent être renseignés ensemble.");
        if (input.PublicationStatus == StudyStatus.Published && (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Slug))) ModelState.AddModelError(key, $"La traduction {key} publiée doit avoir un titre et un slug.");
    }
}




