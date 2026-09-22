using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminChapterQueries(ApplicationDbContext db) : IAdminChapterQueries
{
    public async Task<(AdminChapterCourseDto Course, IReadOnlyList<AdminChapterListDto> Chapters)?> ListAsync(Guid courseId, CancellationToken token = default)
    {
        var course = await db.StudyCourses.AsNoTracking().Where(x => x.Id == courseId).Select(x => new AdminChapterCourseDto(x.Id, x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Title).FirstOrDefault() ?? "Cours sans traduction", x.Thumbnail, x.Level, x.CourseCategory.Title, x.Translations.Where(t => t.LanguageCode == "fr").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == "en").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault())).FirstOrDefaultAsync(token);
        if (course is null) return null;
        var chapters = await db.StudyCourseContents.AsNoTracking().Where(x => x.CourseId == courseId).OrderBy(x => x.Order).ThenBy(x => x.Id).Select(x => new AdminChapterListDto(x.Id, x.Order, x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Title, x.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Title).FirstOrDefault() ?? "Missing", x.Translations.Where(t => t.LanguageCode == "fr").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == "en").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(), x.CourseLessons.Count, x.CourseQuizzes.Count)).ToListAsync(token);
        return (course, chapters);
    }

    public async Task<AdminChapterEditDto?> GetForEditAsync(Guid courseId, Guid? chapterId, CancellationToken token = default)
    {
        var course = await db.StudyCourses.AsNoTracking().Where(x => x.Id == courseId).Select(x => new { x.Translations, x.Title }).FirstOrDefaultAsync(token); if (course is null) return null;
        var c = chapterId.HasValue ? await db.StudyCourseContents.AsNoTracking().Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == chapterId && x.CourseId == courseId, token) : null;
        AdminChapterTranslationDto T(string l) => new(c?.Translations.FirstOrDefault(x => x.LanguageCode == l)?.Title, c?.Translations.FirstOrDefault(x => x.LanguageCode == l)?.Summary, c?.Translations.FirstOrDefault(x => x.LanguageCode == l)?.PublicationStatus ?? StudyStatus.Draft);
        return new(chapterId.HasValue ? (c?.Id ?? Guid.Empty) : Guid.Empty, courseId, c?.Order ?? (await db.StudyCourseContents.CountAsync(x => x.CourseId == courseId, token) + 1), course.Translations.FirstOrDefault(x => x.LanguageCode == "fr")?.Title ?? course.Title, course.Translations.FirstOrDefault(x => x.LanguageCode == "en")?.Title ?? "Missing", T("fr"), T("en"));
    }
}

public sealed class EfAdminChapterPersistence(ApplicationDbContext db, IHtmlSanitizer sanitizer) : IAdminChapterPersistence
{
    public async Task<(bool Success, Guid ChapterId, string? Error)> SaveAsync(AdminChapterSaveCommand m, CancellationToken t = default)
    { var course = await db.StudyCourses.AnyAsync(x => x.Id == m.CourseId, t); if (!course) return (false, Guid.Empty, "Course not found."); var isNew = !m.Id.HasValue || m.Id.Value == Guid.Empty; var e = isNew ? new CourseContent { Id = Guid.NewGuid(), CourseId = m.CourseId, CreatedOnUtc = DateTime.UtcNow } : await db.StudyCourseContents.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == m.Id && x.CourseId == m.CourseId, t); if (e is null) return (false, Guid.Empty, "Chapter not found."); e.Order = Math.Max(1, m.Order); e.Title = m.French.Title ?? m.English.Title ?? "Chapter"; e.Summary = sanitizer.Sanitize(m.French.Summary); e.Status = m.French.PublicationStatus; if (e.Translations is null) { } Upsert(e, "fr", m.French); Upsert(e, "en", m.English); if (isNew) db.StudyCourseContents.Add(e); await NormalizeAsync(m.CourseId, isNew ? e : null, null, t); await db.SaveChangesAsync(t); return (true, e.Id, null); }

    public async Task MoveAsync(Guid courseId, Guid chapterId, int direction, CancellationToken t = default)
    { var a = await db.StudyCourseContents.Where(x => x.CourseId == courseId).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); var i = a.FindIndex(x => x.Id == chapterId); var j = i + direction; if (i < 0 || j < 0 || j >= a.Count) return; (a[i].Order, a[j].Order) = (a[j].Order, a[i].Order); await NormalizeAsync(courseId, null, null, t); await db.SaveChangesAsync(t); }

    public async Task<AdminChapterDeleteResult> DeleteAsync(Guid courseId, Guid chapterId, CancellationToken t = default)
    { var e = await db.StudyCourseContents.Include(x => x.CourseLessons).Include(x => x.CourseQuizzes).FirstOrDefaultAsync(x => x.Id == chapterId && x.CourseId == courseId, t); if (e is null) return new(false, true, "Chapitre introuvable."); if (e.CourseLessons.Count > 0 || e.CourseQuizzes.Count > 0) return new(false, false, "Ce chapitre contient des leçons ou des quiz et ne peut pas être supprimé."); db.StudyCourseContents.Remove(e); await NormalizeAsync(courseId, null, chapterId, t); await db.SaveChangesAsync(t); return new(true, false, "Le chapitre a été supprimé."); }

    private void Upsert(CourseContent e, string l, AdminChapterTranslationDto i)
    { if (string.IsNullOrWhiteSpace(i.Title) && string.IsNullOrWhiteSpace(i.Summary)) return; var x = e.Translations.FirstOrDefault(x => x.LanguageCode == l); if (x is null) { x = new CourseContentTranslation { Id = Guid.NewGuid(), CourseContentId = e.Id }; e.Translations.Add(x); } x.LanguageCode = l; x.Title = i.Title?.Trim() ?? ""; x.Summary = sanitizer.Sanitize(i.Summary); x.PublicationStatus = i.PublicationStatus; }

    private async Task NormalizeAsync(Guid id, CourseContent? pending, Guid? excludedId, CancellationToken t)
    { var a = await db.StudyCourseContents.Where(x => x.CourseId == id && (!excludedId.HasValue || x.Id != excludedId.Value)).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); if (pending is not null && !a.Any(x => x.Id == pending.Id)) a.Add(pending); a = a.OrderBy(x => x.Order).ThenBy(x => x.Id).ToList(); for (var i = 0; i < a.Count; i++) a[i].Order = i + 1; }
}