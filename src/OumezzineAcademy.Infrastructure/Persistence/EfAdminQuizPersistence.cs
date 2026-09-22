using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminQuizQueries(ApplicationDbContext db) : IAdminQuizQueries
{
    public async Task<(AdminQuizContextDto Context, IReadOnlyList<AdminQuizListDto> Quizzes)?> ListAsync(Guid courseId, Guid chapterId, CancellationToken t = default)
    { var c = await db.StudyCourseContents.AsNoTracking().Where(x => x.Id == chapterId && x.CourseId == courseId).Select(x => new AdminQuizContextDto(courseId, chapterId, x.Course.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Course.Title, x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Title, x.Order, x.CourseLessons.Count, x.Translations.Where(t => t.LanguageCode == "fr").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == "en").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault())).FirstOrDefaultAsync(t); if (c is null) return null; var q = await db.StudyCourseQuizzes.AsNoTracking().Where(x => x.CourseContentId == chapterId).OrderBy(x => x.Order).ThenBy(x => x.Id).Select(x => new AdminQuizListDto(x.Id, x.Order, x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Title).FirstOrDefault() ?? "Quiz sans traduction", x.Translations.Where(t => t.LanguageCode == "fr").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == "en").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(), x.QuizQuestions.Count)).ToListAsync(t); return (c, q); }

    public async Task<AdminQuizEditDto?> GetForEditAsync(Guid courseId, Guid chapterId, Guid? quizId, CancellationToken t = default)
    { var c = await db.StudyCourseContents.AsNoTracking().Include(x => x.Course).ThenInclude(x => x.Translations).Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == chapterId && x.CourseId == courseId, t); if (c is null) return null; var e = quizId.HasValue ? await db.StudyCourseQuizzes.AsNoTracking().Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == quizId && x.CourseContentId == chapterId, t) : null; AdminQuizTranslationDto M(string l) { var x = e?.Translations.FirstOrDefault(x => x.LanguageCode == l); return new(x?.Title, x?.Slug, x?.Summary, x?.PublicationStatus ?? StudyStatus.Draft); } return new(e?.Id ?? Guid.Empty, courseId, chapterId, e?.Order ?? (await db.StudyCourseQuizzes.CountAsync(x => x.CourseContentId == chapterId, t) + 1), c.Course.Translations.FirstOrDefault(x => x.LanguageCode == "fr")?.Title ?? c.Course.Title, c.Translations.FirstOrDefault(x => x.LanguageCode == "fr")?.Title ?? c.Title, M("fr"), M("en")); }

    public Task<bool> SlugExistsAsync(string l, string s, Guid id, CancellationToken t = default) => db.StudyCourseQuizTranslations.AnyAsync(x => x.LanguageCode == l && x.Slug == s && x.CourseQuizId != id, t);
}

public sealed class EfAdminQuizPersistence(ApplicationDbContext db) : IAdminQuizPersistence
{
    public async Task<(bool Success, Guid QuizId, string? Error)> SaveAsync(AdminQuizSaveCommand m, CancellationToken t = default)
    { var parent = await db.StudyCourseContents.AnyAsync(x => x.Id == m.ChapterId && x.CourseId == m.CourseId, t); if (!parent) return (false, Guid.Empty, "Chapter not found."); var isNew = !m.Id.HasValue || m.Id.Value == Guid.Empty; var e = isNew ? new CourseQuiz { Id = Guid.NewGuid(), CourseContentId = m.ChapterId, CreatedOnUtc = DateTime.UtcNow } : await db.StudyCourseQuizzes.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == m.Id && x.CourseContentId == m.ChapterId, t); if (e is null) return (false, Guid.Empty, "Quiz not found."); e.Order = Math.Max(1, m.Order); e.Title = m.French.Title ?? m.English.Title ?? "Quiz"; e.Slug = m.French.Slug ?? m.English.Slug ?? $"quiz-{e.Id:N}"; e.Summary = m.French.Summary; e.Status = m.French.PublicationStatus; db.Entry(e).Property<bool>("IsDeleted").CurrentValue = false; Upsert(e, "fr", m.French); Upsert(e, "en", m.English); if (isNew) db.StudyCourseQuizzes.Add(e); await NormalizeAsync(m.ChapterId, isNew ? e : null, null, t); await db.SaveChangesAsync(t); return (true, e.Id, null); }

    public async Task MoveAsync(Guid courseId, Guid chapterId, Guid id, int direction, CancellationToken t = default)
    { var a = await db.StudyCourseQuizzes.Where(x => x.CourseContentId == chapterId && x.CourseContent.CourseId == courseId).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); var i = a.FindIndex(x => x.Id == id); var j = i + direction; if (i < 0 || j < 0 || j >= a.Count) return; (a[i].Order, a[j].Order) = (a[j].Order, a[i].Order); await NormalizeAsync(chapterId, null, null, t); await db.SaveChangesAsync(t); }

    public async Task<AdminQuizDeleteResult> DeleteAsync(Guid courseId, Guid chapterId, Guid id, CancellationToken t = default)
    { var e = await db.StudyCourseQuizzes.Include(x => x.QuizQuestions).FirstOrDefaultAsync(x => x.Id == id && x.CourseContentId == chapterId && x.CourseContent.CourseId == courseId, t); if (e is null) return new(false, true, "Quiz introuvable."); if (e.QuizQuestions.Count > 0) return new(false, false, "Ce quiz contient des questions. Supprimez-les d'abord."); db.StudyCourseQuizzes.Remove(e); await NormalizeAsync(chapterId, null, id, t); await db.SaveChangesAsync(t); return new(true, false, "Le quiz a été supprimé."); }

    private void Upsert(CourseQuiz e, string l, AdminQuizTranslationDto i)
    { if (string.IsNullOrWhiteSpace(i.Title) && string.IsNullOrWhiteSpace(i.Slug)) return; var x = e.Translations.FirstOrDefault(x => x.LanguageCode == l); if (x is null) { x = new CourseQuizTranslation { Id = Guid.NewGuid(), CourseQuizId = e.Id }; e.Translations.Add(x); } x.LanguageCode = l; x.Title = i.Title?.Trim() ?? ""; x.Slug = i.Slug?.Trim() ?? ""; x.Summary = i.Summary; x.PublicationStatus = i.PublicationStatus; }

    private async Task NormalizeAsync(Guid id, CourseQuiz? pending, Guid? excludedId, CancellationToken t)
    { var a = await db.StudyCourseQuizzes.Where(x => x.CourseContentId == id && (!excludedId.HasValue || x.Id != excludedId.Value)).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); if (pending is not null && !a.Any(x => x.Id == pending.Id)) a.Add(pending); a = a.OrderBy(x => x.Order).ThenBy(x => x.Id).ToList(); for (var i = 0; i < a.Count; i++) a[i].Order = i + 1; }
}