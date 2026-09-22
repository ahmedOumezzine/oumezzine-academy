using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;

namespace OumezzineAcademy.Web.Services;

public sealed class AdminQuizService(IAdminQuizQueries queries, IAdminQuizPersistence persistence, IStudyLmsCacheInvalidator cache)
{
    public async Task<AdminQuizListViewModel?> ListAsync(Guid courseId, Guid chapterId, CancellationToken t)
    { var r = await queries.ListAsync(courseId, chapterId, t); if (r is null) return null; var (c, rows) = r.Value; var q = rows.Select((x, i) => new AdminQuizListItem { Id = x.Id, Order = x.Order, Title = x.Title, FrenchStatus = x.FrenchStatus, EnglishStatus = x.EnglishStatus, QuestionsCount = x.QuestionsCount, IsFirst = i == 0, IsLast = i == rows.Count - 1 }).ToList(); return new() { CourseId = courseId, ChapterId = chapterId, CourseTitle = c.CourseTitle, ChapterTitle = c.ChapterTitle, ChapterOrder = c.ChapterOrder, LessonsCount = c.LessonsCount, FrenchStatus = c.FrenchStatus, EnglishStatus = c.EnglishStatus, Quizzes = q }; }

    public async Task<AdminQuizFormViewModel?> GetFormAsync(Guid courseId, Guid chapterId, Guid? id, CancellationToken t)
    { var x = await queries.GetForEditAsync(courseId, chapterId, id, t); if (x is null) return null; static QuizTranslationInput M(AdminQuizTranslationDto x) => new() { Title = x.Title, Slug = x.Slug, Summary = x.Summary, PublicationStatus = x.PublicationStatus }; return new() { Id = x.Id, CourseId = x.CourseId, ChapterId = x.ChapterId, Order = x.Order, CourseTitle = x.CourseTitle, ChapterTitle = x.ChapterTitle, French = M(x.French), English = M(x.English) }; }

    public async Task<Guid> SaveAsync(AdminQuizFormViewModel m, CancellationToken t)
    { static AdminQuizTranslationDto M(QuizTranslationInput x) => new(x.Title, x.Slug, x.Summary, x.PublicationStatus); var r = await persistence.SaveAsync(new AdminQuizSaveCommand(m.Id == Guid.Empty ? null : m.Id, m.CourseId, m.ChapterId, m.Order, M(m.French), M(m.English)), t); if (!r.Success) throw new InvalidOperationException(r.Error); await cache.InvalidatePublicAsync(t); return r.QuizId; }

    public async Task MoveAsync(Guid courseId, Guid chapterId, Guid id, int direction, CancellationToken t)
    { await persistence.MoveAsync(courseId, chapterId, id, direction, t); await cache.InvalidatePublicAsync(t); }

    public async Task DeleteAsync(Guid courseId, Guid chapterId, Guid id, CancellationToken t)
    { var r = await persistence.DeleteAsync(courseId, chapterId, id, t); if (r.NotFound) throw new KeyNotFoundException(); if (!r.Success) throw new InvalidOperationException(r.Message); await cache.InvalidatePublicAsync(t); }

    public Task<bool> SlugExistsAsync(string l, string s, Guid id, CancellationToken t) => queries.SlugExistsAsync(l, s, id, t);
}