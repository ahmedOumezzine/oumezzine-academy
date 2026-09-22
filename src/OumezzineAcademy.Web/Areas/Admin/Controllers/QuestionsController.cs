using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class QuestionsController : Controller
{
    private readonly AdminQuestionService _service;

    public QuestionsController(AdminQuestionService service)
    {
        _service = service;
    }

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{quizId:guid}/questions")]
    public async Task<IActionResult> Index(Guid courseId, Guid chapterId, Guid quizId, CancellationToken token)
    {
        var model = await _service.ListAsync(courseId, chapterId, quizId, token);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{quizId:guid}/questions/create")]
    public async Task<IActionResult> Create(Guid courseId, Guid chapterId, Guid quizId, CancellationToken token)
    {
        var model = await _service.GetFormAsync(courseId, chapterId, quizId, null, token);
        return model is null ? NotFound() : View("Edit", model);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{quizId:guid}/questions/create")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(Guid courseId, Guid chapterId, Guid quizId, AdminQuestionFormViewModel model, CancellationToken token)
    {
        return Save(courseId, chapterId, quizId, model, token);
    }

    [HttpGet("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{quizId:guid}/questions/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid courseId, Guid chapterId, Guid quizId, Guid id, CancellationToken token)
    {
        var model = await _service.GetFormAsync(courseId, chapterId, quizId, id, token);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{quizId:guid}/questions/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(Guid courseId, Guid chapterId, Guid quizId, Guid id, AdminQuestionFormViewModel model, CancellationToken token)
    {
        model.Id = id;
        return Save(courseId, chapterId, quizId, model, token);
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{quizId:guid}/questions/{id:guid}/move")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(Guid courseId, Guid chapterId, Guid quizId, Guid id, int direction, CancellationToken token)
    {
        await _service.MoveAsync(courseId, chapterId, quizId, id, direction, token);
        return RedirectToAction(nameof(Index), new { courseId, chapterId, quizId });
    }

    [HttpPost("admin/courses/{courseId:guid}/chapters/{chapterId:guid}/quizzes/{quizId:guid}/questions/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid courseId, Guid chapterId, Guid quizId, Guid id, CancellationToken token)
    {
        try
        {
            await _service.DeleteAsync(courseId, chapterId, quizId, id, token);
            TempData["Success"] = "Question supprimée.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { courseId, chapterId, quizId });
    }

    private async Task<IActionResult> Save(Guid courseId, Guid chapterId, Guid quizId, AdminQuestionFormViewModel model, CancellationToken token)
    {
        model.CourseId = courseId;
        model.ChapterId = chapterId;
        model.QuizId = quizId;

        if (model.French.PublicationStatus == StudyStatus.Published && string.IsNullOrWhiteSpace(model.French.Text))
            ModelState.AddModelError("French.Text", "Le texte français est requis pour publier.");

        if (model.English.PublicationStatus == StudyStatus.Published && string.IsNullOrWhiteSpace(model.English.Text))
            ModelState.AddModelError("English.Text", "Le texte anglais est requis pour publier.");

        if (!ModelState.IsValid)
            return View("Edit", model);

        try
        {
            await _service.SaveAsync(model, token);
            TempData["Success"] = "Question enregistrée.";
            return RedirectToAction(nameof(Index), new { courseId, chapterId, quizId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Edit", model);
        }
    }
}


