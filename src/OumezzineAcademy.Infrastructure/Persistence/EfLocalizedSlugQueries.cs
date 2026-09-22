using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfLocalizedSlugQueries(ApplicationDbContext db) : ILocalizedSlugQueries
{
    public async Task<LocalizedSlugSet?> FindAsync(string entityType, string slug, string languageCode, CancellationToken cancellationToken = default)
    {
        return entityType switch
        {
            "course" => await CourseAsync(slug, languageCode, cancellationToken),
            "category" => await CategoryAsync(slug, languageCode, cancellationToken),
            "path" => await PathAsync(slug, languageCode, cancellationToken),
            "lesson" => await LessonAsync(slug, languageCode, cancellationToken),
            "quiz" => await QuizAsync(slug, languageCode, cancellationToken),
            _ => null
        };
    }

    private async Task<LocalizedSlugSet?> CourseAsync(string slug, string language, CancellationToken token)
    {
        var id = await db.StudyCourseTranslations.AsNoTracking().Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published && t.Slug == slug && t.Course.Status == StudyStatus.Published).Select(t => (Guid?)t.CourseId).FirstOrDefaultAsync(token);
        return id is null ? null : new(id.Value, await db.StudyCourseTranslations.AsNoTracking().Where(t => t.CourseId == id && t.PublicationStatus == StudyStatus.Published).ToDictionaryAsync(t => t.LanguageCode, t => t.Slug, token));
    }

    private async Task<LocalizedSlugSet?> CategoryAsync(string slug, string language, CancellationToken token)
    {
        var id = await db.StudyCourseCategoryTranslations.AsNoTracking().Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published && t.Slug == slug && t.CourseCategory.Status == StudyStatus.Published).Select(t => (Guid?)t.CourseCategoryId).FirstOrDefaultAsync(token);
        return id is null ? null : new(id.Value, await db.StudyCourseCategoryTranslations.AsNoTracking().Where(t => t.CourseCategoryId == id && t.PublicationStatus == StudyStatus.Published).ToDictionaryAsync(t => t.LanguageCode, t => t.Slug, token));
    }

    private async Task<LocalizedSlugSet?> PathAsync(string slug, string language, CancellationToken token)
    {
        var id = await db.StudyLearningPathTranslations.AsNoTracking().Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published && t.Slug == slug && t.LearningPath.Status == StudyStatus.Published).Select(t => (Guid?)t.LearningPathId).FirstOrDefaultAsync(token);
        return id is null ? null : new(id.Value, await db.StudyLearningPathTranslations.AsNoTracking().Where(t => t.LearningPathId == id && t.PublicationStatus == StudyStatus.Published).ToDictionaryAsync(t => t.LanguageCode, t => t.Slug, token));
    }

    private async Task<LocalizedSlugSet?> LessonAsync(string slug, string language, CancellationToken token)
    {
        var id = await db.StudyCourseLessonTranslations.AsNoTracking().Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published && t.Slug == slug && t.CourseLesson.Status == StudyStatus.Published && t.CourseLesson.CourseContent.Course.Status == StudyStatus.Published).Select(t => (Guid?)t.CourseLessonId).FirstOrDefaultAsync(token);
        return id is null ? null : new(id.Value, await db.StudyCourseLessonTranslations.AsNoTracking().Where(t => t.CourseLessonId == id && t.PublicationStatus == StudyStatus.Published).ToDictionaryAsync(t => t.LanguageCode, t => t.Slug, token));
    }

    private async Task<LocalizedSlugSet?> QuizAsync(string slug, string language, CancellationToken token)
    {
        var id = await db.StudyCourseQuizTranslations.AsNoTracking().Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published && t.Slug == slug && t.CourseQuiz.Status == StudyStatus.Published && t.CourseQuiz.CourseContent.Course.Status == StudyStatus.Published).Select(t => (Guid?)t.CourseQuizId).FirstOrDefaultAsync(token);
        return id is null ? null : new(id.Value, await db.StudyCourseQuizTranslations.AsNoTracking().Where(t => t.CourseQuizId == id && t.PublicationStatus == StudyStatus.Published).ToDictionaryAsync(t => t.LanguageCode, t => t.Slug, token));
    }
}