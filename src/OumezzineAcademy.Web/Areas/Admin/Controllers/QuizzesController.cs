using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class QuizzesController : Controller
{
    private readonly AdminQuizService _service;

    public QuizzesController(AdminQuizService service) => _service = service;

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes")]
    public async Task<IActionResult> Index(Guid courseId, Guid chapterId, CancellationToken token)
    {
        var model = await _service.ListAsync(courseId, chapterId, token);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/create")]
    public async Task<IActionResult> Create(Guid courseId, Guid chapterId, CancellationToken token)
    {
        var model = await _service.GetFormAsync(courseId, chapterId, null, token);
        return model is null ? NotFound() : View("Edit", model);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/create"), ValidateAntiForgeryToken]
    public Task<IActionResult> Create(Guid courseId, Guid chapterId, AdminQuizFormViewModel model, CancellationToken token) => Save(courseId, chapterId, model, token);

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid courseId, Guid chapterId, Guid id, CancellationToken token)
    {
        var model = await _service.GetFormAsync(courseId, chapterId, id, token);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{id:guid}/edit"), ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(Guid courseId, Guid chapterId, Guid id, AdminQuizFormViewModel model, CancellationToken token)
    {
        model.Id = id;
        return Save(courseId, chapterId, model, token);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{id:guid}/move"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(Guid courseId, Guid chapterId, Guid id, int direction, CancellationToken token)
    {
        await _service.MoveAsync(courseId, chapterId, id, direction, token);
        return RedirectToAction(nameof(Index), new { courseId, chapterId });
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{id:guid}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid courseId, Guid chapterId, Guid id, CancellationToken token)
    {
        try
        {
            await _service.DeleteAsync(courseId, chapterId, id, token); TempData["Success"] = "Quiz supprimé.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { courseId, chapterId });
    }

    private async Task<IActionResult> Save(Guid courseId, Guid chapterId, AdminQuizFormViewModel model, CancellationToken token)
    {
        model.CourseId = courseId; model.ChapterId = chapterId; Validate(model.French, "French"); Validate(model.English, "English");
        if (!string.IsNullOrWhiteSpace(model.French.Slug) && await _service.SlugExistsAsync("fr", model.French.Slug, model.Id, token)) ModelState.AddModelError("French.Slug", "Ce slug existe déjà en français.");
        if (!string.IsNullOrWhiteSpace(model.English.Slug) && await _service.SlugExistsAsync("en", model.English.Slug, model.Id, token)) ModelState.AddModelError("English.Slug", "Ce slug existe déjà en anglais.");
        if (!ModelState.IsValid) return View("Edit", model);
        try
        {
            await _service.SaveAsync(model, token); TempData["Success"] = "Quiz enregistré.";
            return RedirectToAction(nameof(Index), new { courseId, chapterId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Edit", model);
        }
    }

    private void Validate(QuizTranslationInput input, string key)
    {
        if (input.PublicationStatus == StudyStatus.Published && (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Slug) || string.IsNullOrWhiteSpace(input.Summary))) ModelState.AddModelError(key, $"La traduction {key} publiée doit avoir un titre, un slug et un résumé.");
    }
}