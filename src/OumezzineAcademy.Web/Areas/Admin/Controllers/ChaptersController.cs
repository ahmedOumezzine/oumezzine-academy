using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class ChaptersController : Controller
{
    private readonly AdminChapterService _service;

    public ChaptersController(AdminChapterService service) => _service = service;

    [HttpGet("admin/courses/{courseId:guid}/chapters")]
    public async Task<IActionResult> Index(Guid courseId, CancellationToken token)
    {
        var result = await _service.ListAsync(courseId, token);
        return result is null ? NotFound() : View(result);
    }

    [HttpGet("admin/courses/{courseId:guid}/chapters/create")]
    public async Task<IActionResult> Create(Guid courseId, CancellationToken token)
    {
        var model = await _service.GetFormAsync(courseId, null, token);
        return model is null ? NotFound() : View("Edit", model);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid courseId, ChapterEditViewModel model, CancellationToken token) => await Save(courseId, model, token);

    [HttpGet("admin/courses/{courseId:guid}/chapters/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid courseId, Guid id, CancellationToken token)
    {
        var model = await _service.GetFormAsync(courseId, id, token);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{id:guid}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid courseId, Guid id, ChapterEditViewModel model, CancellationToken token)
    { model.Id = id;
        return await Save(courseId, model, token);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{id:guid}/move"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(Guid courseId, Guid id, int direction, CancellationToken token)
    { await _service.MoveAsync(courseId, id, direction, token);
        return RedirectToAction(nameof(Index), new { courseId });
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{id:guid}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid courseId, Guid id, CancellationToken token)
    {
        try
        { await _service.DeleteAsync(courseId, id, token); TempData["Success"] = "Chapitre supprimé.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { courseId });
    }

    private async Task<IActionResult> Save(Guid courseId, ChapterEditViewModel model, CancellationToken token)
    { model.CourseId = courseId; Validate(model.French, "French"); Validate(model.English, "English");
        if (!ModelState.IsValid) {
        var form = await _service.GetFormAsync(courseId, model.Id == Guid.Empty ? null : model.Id, token);
        if (form is not null) { model.CourseFrenchTitle = form.CourseFrenchTitle; model.CourseEnglishTitle = form.CourseEnglishTitle;
        }
        return View("Edit", model);
    } try { await _service.SaveAsync(model, token); TempData["Success"] = "Chapitre enregistré.";
        if (Request.Form["continueEditing"] == "true")
            return RedirectToAction(nameof(Edit), new { courseId, id = model.Id });
        return RedirectToAction(nameof(Index), new { courseId });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message);
        return View("Edit", model);
    } }

    private void Validate(ChapterTranslationInput input, string key)
    {
        if (input.PublicationStatus == StudyStatus.Published && (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Summary))) ModelState.AddModelError(key, "Une traduction publiée doit avoir un titre et un résumé.");
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{id:guid}/image"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(Guid courseId, Guid id, IFormFile file, CancellationToken token)
    {
        try
        {
            var url = await _service.UploadImageAsync(courseId, id, file, token);
            return Ok(new { url, alt = System.IO.Path.GetFileNameWithoutExtension(file.FileName) });
        }
        catch (InvalidDataException exception)
        {
            ModelState.AddModelError(nameof(file), exception.Message);
            return ValidationProblem(ModelState);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}


