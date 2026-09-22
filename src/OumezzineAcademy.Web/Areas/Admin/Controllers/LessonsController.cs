using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class LessonsController : Controller
{
    private readonly AdminLessonService _service;

    public LessonsController(AdminLessonService service)
    {
        _service = service;
    }

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/lessons")]
    public async Task<IActionResult> Index(Guid courseId, Guid chapterId, CancellationToken token)
    {
        var result = await _service.ListAsync(courseId, chapterId, token);
        return result is null ? NotFound() : View(result);
    }

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/lessons/create")]
    public async Task<IActionResult> Create(Guid courseId, Guid chapterId, CancellationToken token)
    {
        var model = await _service.GetFormAsync(courseId, chapterId, null, token);
        return model is null ? NotFound() : View("Edit", model);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/lessons/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid courseId, Guid chapterId, LessonEditViewModel model, CancellationToken token) => await Save(courseId, chapterId, model, token);

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/lessons/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid courseId, Guid chapterId, Guid id, CancellationToken token)
    {
        var model = await _service.GetFormAsync(courseId, chapterId, id, token);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/lessons/{id:guid}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid courseId, Guid chapterId, Guid id, LessonEditViewModel model, CancellationToken token)
    {
        model.Id = id;
        return await Save(courseId, chapterId, model, token);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/lessons/{id:guid}/move"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(Guid courseId, Guid chapterId, Guid id, int direction, CancellationToken token)
    {
        await _service.MoveAsync(courseId, chapterId, id, direction, token);
        return RedirectToAction(nameof(Index), new { courseId, chapterId });
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/lessons/{id:guid}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid courseId, Guid chapterId, Guid id, CancellationToken token)
    {
        try
        {
            await _service.DeleteAsync(courseId, chapterId, id, token); TempData["Success"] = "Leçon supprimée.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index), new { courseId, chapterId });
    }

    private async Task<IActionResult> Save(Guid courseId, Guid chapterId, LessonEditViewModel model, CancellationToken token)
    {
        model.CourseId = courseId; model.ChapterId = chapterId; Validate(model.French, "French"); Validate(model.English, "English");
        if (!string.IsNullOrWhiteSpace(model.French.Slug) && await _service.SlugExistsAsync("fr", model.French.Slug, model.Id, token)) ModelState.AddModelError("French.Slug", "Ce slug existe déjà en français.");
        if (!string.IsNullOrWhiteSpace(model.English.Slug) && await _service.SlugExistsAsync("en", model.English.Slug, model.Id, token)) ModelState.AddModelError("English.Slug", "Ce slug existe déjà en anglais.");
        if (!ModelState.IsValid) return View("Edit", model);
        try
        {
            var lessonId = await _service.SaveAsync(model, token);
            model.Id = lessonId;
            ModelState.Remove(nameof(model.Id));
            if (model.LessonImage is not null)
            {
                var url = await _service.UploadImageAsync(lessonId, model.LessonImage, token);
                var encoder = System.Text.Encodings.Web.HtmlEncoder.Default;
                var alt = System.IO.Path.GetFileNameWithoutExtension(model.LessonImage.FileName);
                model.French.ContentHtml += $"<figure><img src=\"{encoder.Encode(url)}\" alt=\"{encoder.Encode(alt)}\"><figcaption></figcaption></figure>";
                await _service.SaveAsync(model, token);
            }
            TempData["Success"] = "Leçon enregistrée.";
            if (Request.Form["continueEditing"] == "true")
                return RedirectToAction(nameof(Edit), new { courseId, chapterId, id = lessonId });
            return RedirectToAction(nameof(Index), new { courseId, chapterId });
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError(nameof(model.LessonImage), ex.Message);
            return View("Edit", model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Edit", model);
        }
    }

    private void Validate(LessonTranslationInput input, string key)
    {
        if (input.PublicationStatus == StudyStatus.Published && (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Slug) || string.IsNullOrWhiteSpace(input.Summary))) ModelState.AddModelError(key, "Une traduction publiée doit avoir titre, slug et résumé.");
        if (!string.IsNullOrWhiteSpace(input.VideoUrl) && !IsWebUrl(input.VideoUrl)) ModelState.AddModelError($"{key}.VideoUrl", "L'URL vidéo doit être une URL HTTP ou HTTPS valide.");
        if (!string.IsNullOrWhiteSpace(input.DocumentUrl) && !IsWebUrl(input.DocumentUrl)) ModelState.AddModelError($"{key}.DocumentUrl", "L'URL document doit être une URL HTTP ou HTTPS valide.");
    }

    private static bool IsWebUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    [HttpPost("image/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var url = await _service.UploadImageAsync(id, file, cancellationToken);
            return Ok(new { url, alt = Path.GetFileNameWithoutExtension(file.FileName) });
        }
        catch (InvalidDataException exception)
        {
            ModelState.AddModelError(nameof(file), exception.Message);
            return ValidationProblem(ModelState);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("image/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid id, [FromForm] string url, CancellationToken cancellationToken)
    {
        try { await _service.DeleteImageAsync(id, url, cancellationToken); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException) { return BadRequest(); }
        return Ok();
    }
}