using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class CategoriesController : Controller
{
    private readonly AdminCategoryService _service;

    public CategoriesController(AdminCategoryService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(CancellationToken token)
    {
        var model = await _service.ListAsync(token);
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryEditViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(CategoryEditViewModel model, CancellationToken token)
    {
        return Save(model, token);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken token)
    {
        var model = await _service.FindAsync(id, token);
        if (model is null)
        {
            return NotFound();
        }

        return View("Create", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(CategoryEditViewModel model, CancellationToken token)
    {
        return Save(model, token);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        var result = await _service.DeleteAsync(id, token);
        if (result == OumezzineAcademy.Application.Abstractions.AdminCategoryDeleteStatus.NotFound) return NotFound();
        if (result == OumezzineAcademy.Application.Abstractions.AdminCategoryDeleteStatus.InUse) { TempData["Error"] = "Cette catégorie contient des cours et ne peut pas être supprimée."; return RedirectToAction(nameof(Index)); }
        TempData["Success"] = "Catégorie supprimée.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(CategoryEditViewModel model, CancellationToken token)
    {
        Validate(model.French, "French");
        Validate(model.English, "English");
        if (!string.IsNullOrWhiteSpace(model.French.Slug) && await _service.SlugExistsAsync("fr", model.French.Slug, model.Id, token)) ModelState.AddModelError("French.Slug", "Ce slug existe déjà en français.");
        if (!string.IsNullOrWhiteSpace(model.English.Slug) && await _service.SlugExistsAsync("en", model.English.Slug, model.Id, token)) ModelState.AddModelError("English.Slug", "Ce slug existe déjà en anglais.");
        if (!ModelState.IsValid)
        {
            return View("Create", model);
        }

        try
        {
            await _service.SaveAsync(model, token);
            TempData["Success"] = "Catégorie enregistrée.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError("", "Impossible d'enregistrer cette catégorie. Vérifiez les slugs.");
            return View("Create", model);
        }
    }

    private void Validate(CategoryTranslationInput input, string language)
    {
        if (string.IsNullOrWhiteSpace(input.Title) != string.IsNullOrWhiteSpace(input.Slug))
        {
            ModelState.AddModelError(language, "Le titre et le slug doivent être renseignés ensemble.");
        }

        if (input.PublicationStatus == StudyStatus.Published &&
            (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Slug)))
        {
            ModelState.AddModelError(language, "Une traduction publiée doit avoir un titre et un slug.");
        }
    }
}