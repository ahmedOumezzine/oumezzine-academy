using OumezzineAcademy.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace OumezzineAcademy.Controllers;

public class QuizController : Controller
{
    private readonly IQuizService _quiz;

    public QuizController(IQuizService quiz)
    {
        _quiz = quiz;
    }

    public async Task<IActionResult> Attempt(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();
        var model = await _quiz.GetQuizAsync(slug);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid quizId, IFormCollection form)
    {
        if (quizId == Guid.Empty) return BadRequest();
        var model = await _quiz.GradeAsync(quizId, form);
        return model == null ? NotFound() : View("Result", model);
    }
}

