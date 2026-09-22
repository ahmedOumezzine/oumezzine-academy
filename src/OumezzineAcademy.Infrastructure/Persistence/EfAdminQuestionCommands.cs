using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminQuestionCommands(ApplicationDbContext db) : IAdminQuestionCommands
{
    public async Task<Guid> SaveAsync(AdminQuestionSaveCommand m, CancellationToken t = default)
    { var quiz = await db.StudyCourseQuizzes.FirstOrDefaultAsync(x => x.Id == m.QuizId && x.CourseContentId == m.ChapterId && x.CourseContent.CourseId == m.CourseId, t) ?? throw new InvalidOperationException("Quiz not found."); var answers = m.Answers.Where(x => !string.IsNullOrWhiteSpace(x.FrenchText) || !string.IsNullOrWhiteSpace(x.EnglishText)).ToList(); if (answers.Count < 2 || answers.Count(x => x.IsCorrect) != 1) throw new InvalidOperationException("Une question doit avoir au moins deux réponses et exactement une bonne réponse."); var e = m.Id.HasValue && m.Id.Value != Guid.Empty ? await db.StudyQuizQuestions.Include(x => x.Translations).Include(x => x.Answers).ThenInclude(x => x.Translations).FirstOrDefaultAsync(x => x.Id == m.Id && x.CourseQuizId == quiz.Id, t) : null; if (e is null) e = new QuizQuestion { Id = Guid.NewGuid(), CourseQuizId = quiz.Id, CreatedOnUtc = DateTime.UtcNow }; e.Order = Math.Max(1, m.Order); e.Text = m.FrenchText ?? m.EnglishText ?? "Question"; Upsert(e, "fr", m.FrenchText, m.FrenchStatus); Upsert(e, "en", m.EnglishText, m.EnglishStatus); if (m.Id is null || m.Id == Guid.Empty) db.StudyQuizQuestions.Add(e); var kept = answers.Where(x => x.Id != Guid.Empty).Select(x => x.Id).ToHashSet(); db.StudyQuizAnswers.RemoveRange(e.Answers.Where(x => x.Id != Guid.Empty && !kept.Contains(x.Id))); foreach (var a in answers) { var x = a.Id == Guid.Empty ? new QuizAnswer { Id = Guid.NewGuid(), QuizQuestionId = e.Id, CreatedOnUtc = DateTime.UtcNow } : e.Answers.FirstOrDefault(x => x.Id == a.Id) ?? throw new InvalidOperationException("Une réponse sélectionnée n'appartient pas à cette question."); x.Text = a.FrenchText ?? a.EnglishText ?? ""; x.IsCorrect = a.IsCorrect; if (a.Id == Guid.Empty) db.StudyQuizAnswers.Add(x); Upsert(x, "fr", a.FrenchText); Upsert(x, "en", a.EnglishText); } await db.SaveChangesAsync(t); return e.Id; }

    private void Upsert(QuizQuestion e, string l, string? text, StudyStatus st)
    { if (string.IsNullOrWhiteSpace(text)) return; var x = e.Translations.FirstOrDefault(x => x.LanguageCode == l) ?? new QuizQuestionTranslation { Id = Guid.NewGuid(), QuizQuestionId = e.Id, LanguageCode = l }; if (x.Id != Guid.Empty && !e.Translations.Contains(x)) e.Translations.Add(x); x.Text = text.Trim(); x.PublicationStatus = st; }

    private void Upsert(QuizAnswer e, string l, string? text)
    { if (string.IsNullOrWhiteSpace(text)) return; var x = e.Translations.FirstOrDefault(x => x.LanguageCode == l) ?? new QuizAnswerTranslation { Id = Guid.NewGuid(), QuizAnswerId = e.Id, LanguageCode = l }; if (x.Id != Guid.Empty && !e.Translations.Contains(x)) e.Translations.Add(x); x.Text = text.Trim(); x.PublicationStatus = StudyStatus.Published; }

    public async Task MoveAsync(AdminQuestionMoveCommand c, CancellationToken t = default)
    { var items = await db.StudyQuizQuestions.Where(x => x.CourseQuizId == c.QuizId && x.CourseQuiz.CourseContentId == c.ChapterId && x.CourseQuiz.CourseContent.CourseId == c.CourseId).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); var i = items.FindIndex(x => x.Id == c.QuestionId); var j = i + c.Direction; if (i < 0 || j < 0 || j >= items.Count) return; (items[i].Order, items[j].Order) = (items[j].Order, items[i].Order); for (var k = 0; k < items.Count; k++) items[k].Order = k + 1; await db.SaveChangesAsync(t); }

    public async Task<AdminQuestionDeleteStatus> DeleteAsync(Guid courseId, Guid chapterId, Guid quizId, Guid questionId, CancellationToken token = default)
    { var e = await db.StudyQuizQuestions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == questionId && x.CourseQuizId == quizId && x.CourseQuiz.CourseContentId == chapterId && x.CourseQuiz.CourseContent.CourseId == courseId, token); if (e is null) return AdminQuestionDeleteStatus.NotFound; db.StudyQuizQuestions.Remove(e); await db.SaveChangesAsync(token); var items = await db.StudyQuizQuestions.Where(x => x.CourseQuizId == quizId).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(token); for (var i = 0; i < items.Count; i++) items[i].Order = i + 1; await db.SaveChangesAsync(token); return AdminQuestionDeleteStatus.Deleted; }
}