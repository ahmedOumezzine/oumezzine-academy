using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;

namespace OumezzineAcademy.Web.Services;

public sealed class AdminQuestionService(IAdminQuestionQueries queries, IAdminQuestionCommands commands, IStudyLmsCacheInvalidator? cache = null)
{
    private readonly IStudyLmsCacheInvalidator _cache = cache ?? new NullStudyLmsCacheInvalidator();

    public async Task<AdminQuestionListViewModel?> ListAsync(Guid courseId, Guid chapterId, Guid quizId, CancellationToken token)
    {
        var d = await queries.ListAsync(courseId, chapterId, quizId, token); if (d is null) return null;
        var items = d.Questions.Select(x => new AdminQuestionListItem { Id = x.Id, Order = x.Order, Text = x.Text, FrenchStatus = x.FrenchStatus, EnglishStatus = x.EnglishStatus, AnswersCount = x.AnswersCount, HasCorrectAnswer = x.HasCorrectAnswer }).ToList();
        for (var i = 0; i < items.Count; i++) { items[i].IsFirst = i == 0; items[i].IsLast = i == items.Count - 1; }
        return new() { CourseId = d.CourseId, ChapterId = d.ChapterId, QuizId = d.QuizId, CourseTitle = d.CourseTitle, ChapterTitle = d.ChapterTitle, QuizTitle = d.QuizTitle, FrenchStatus = d.FrenchStatus, EnglishStatus = d.EnglishStatus, Questions = items };
    }

    public async Task<AdminQuestionFormViewModel?> GetFormAsync(Guid courseId, Guid chapterId, Guid quizId, Guid? id, CancellationToken token)
    {
        var d = await queries.GetFormAsync(courseId, chapterId, quizId, id, token); if (d is null) return null;
        var answers = d.Answers.Select(x => new AdminAnswerInput { Id = x.Id, FrenchText = x.FrenchText, EnglishText = x.EnglishText, IsCorrect = x.IsCorrect }).ToList();
        return new() { Id = d.Id, CourseId = d.CourseId, ChapterId = d.ChapterId, QuizId = d.QuizId, Order = d.Order, CourseTitle = d.CourseTitle, ChapterTitle = d.ChapterTitle, QuizTitle = d.QuizTitle, French = new() { Text = d.FrenchText, PublicationStatus = d.FrenchStatus }, English = new() { Text = d.EnglishText, PublicationStatus = d.EnglishStatus }, Answers = answers, CorrectAnswerIndex = answers.FindIndex(x => x.IsCorrect) };
    }

    public async Task<Guid> SaveAsync(AdminQuestionFormViewModel model, CancellationToken token)
    {
        if (model.CorrectAnswerIndex >= 0 && model.CorrectAnswerIndex < model.Answers.Count) model.Answers[model.CorrectAnswerIndex].IsCorrect = true;
        for (var i = 0; i < model.Answers.Count; i++) if (i != model.CorrectAnswerIndex) model.Answers[i].IsCorrect = false;
        var answers = model.Answers.Where(x => !string.IsNullOrWhiteSpace(x.FrenchText) || !string.IsNullOrWhiteSpace(x.EnglishText)).ToList();
        if (answers.Count < 2 || answers.Count(x => x.IsCorrect) != 1) throw new InvalidOperationException("Une question doit avoir au moins deux réponses et exactement une bonne réponse.");
        var id = await commands.SaveAsync(new AdminQuestionSaveCommand(model.Id == Guid.Empty ? null : model.Id, model.CourseId, model.ChapterId, model.QuizId, model.Order, model.French.Text, model.French.PublicationStatus, model.English.Text, model.English.PublicationStatus, answers.Select(x => new AdminAnswerDto(x.Id, x.FrenchText, x.EnglishText, x.IsCorrect)).ToList()), token);
        await _cache.InvalidatePublicAsync(token); return id;
    }

    public async Task MoveAsync(Guid courseId, Guid chapterId, Guid quizId, Guid id, int direction, CancellationToken token)
    { await commands.MoveAsync(new AdminQuestionMoveCommand(courseId, chapterId, quizId, id, direction), token); await _cache.InvalidatePublicAsync(token); }

    public async Task DeleteAsync(Guid courseId, Guid chapterId, Guid quizId, Guid id, CancellationToken token)
    { if (await commands.DeleteAsync(courseId, chapterId, quizId, id, token) == AdminQuestionDeleteStatus.NotFound) throw new KeyNotFoundException(); await _cache.InvalidatePublicAsync(token); }
}