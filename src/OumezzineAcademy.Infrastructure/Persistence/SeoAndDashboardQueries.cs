using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfSitemapQueries(ApplicationDbContext db) : ISitemapQueries
{
    public async Task<SitemapSlugs> GetSlugsAsync(string languageCode, CancellationToken cancellationToken = default)
        => new(
            await db.StudyCourses.AsNoTracking().Where(c => c.Status == Domain.Catalog.StudyStatus.Published).SelectMany(c => c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == Domain.Catalog.StudyStatus.Published).Select(t => t.Slug)).ToListAsync(cancellationToken),
            await db.StudyCourseCategories.AsNoTracking().Where(c => c.Status == Domain.Catalog.StudyStatus.Published).SelectMany(c => c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == Domain.Catalog.StudyStatus.Published).Select(t => t.Slug)).ToListAsync(cancellationToken),
            await db.StudyLearningPaths.AsNoTracking().Where(p => p.Status == Domain.Catalog.StudyStatus.Published).SelectMany(p => p.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == Domain.Catalog.StudyStatus.Published).Select(t => t.Slug)).ToListAsync(cancellationToken),
            await db.StudyCourseLessons.AsNoTracking().Where(l => l.Status == Domain.Catalog.StudyStatus.Published && l.CourseContent.Course.Status == Domain.Catalog.StudyStatus.Published).SelectMany(l => l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == Domain.Catalog.StudyStatus.Published).Select(t => t.Slug)).ToListAsync(cancellationToken));
}

public sealed class EfDashboardQueries(ApplicationDbContext db) : IDashboardQueries
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken token = default)
    {
        var courses = await db.StudyCourses.AsNoTracking().Select(x => new DashboardRecentItem("Cours", x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Title, "modifié", x.LastModifiedOnUtc ?? x.CreatedOnUtc, x.Id, null, "book")).Take(5).ToListAsync(token);
        var categories = await db.StudyCourseCategories.AsNoTracking().Select(x => new DashboardRecentItem("Catégorie", x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Title, "modifiée", x.LastModifiedOnUtc ?? x.CreatedOnUtc, null, null, "folder2")).Take(5).ToListAsync(token);
        var lessons = await db.StudyCourseLessons.AsNoTracking().Select(x => new DashboardRecentItem("Leçon", x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Title, "modifiée", x.LastModifiedOnUtc ?? x.CreatedOnUtc, x.CourseContent.CourseId, x.CourseContentId, "mortarboard")).Take(5).ToListAsync(token);
        return new(await db.StudyCourses.CountAsync(token), await db.StudyCourseCategories.CountAsync(token), await db.StudyCourses.CountAsync(x => x.Translations.Any(t => t.LanguageCode == "fr" && t.PublicationStatus == Domain.Catalog.StudyStatus.Published), token), await db.StudyCourses.CountAsync(x => x.Translations.Any(t => t.LanguageCode == "en" && t.PublicationStatus == Domain.Catalog.StudyStatus.Published), token), await db.StudyCourses.CountAsync(x => x.Translations.Any(t => t.LanguageCode == "en" && t.PublicationStatus == Domain.Catalog.StudyStatus.Draft), token), await db.StudyCourses.CountAsync(x => !x.Translations.Any(t => t.LanguageCode == "en"), token), await db.StudyCourseLessons.CountAsync(token), await db.StudyCourseQuizzes.CountAsync(token), await db.StudyLearningPaths.CountAsync(token), await db.StudyCourses.CountAsync(x => x.Thumbnail == null || x.Thumbnail == "", token), await db.StudyCourseLessons.CountAsync(x => x.Status == Domain.Catalog.StudyStatus.Draft, token), await db.StudyCourseLessons.CountAsync(x => !x.Translations.Any(t => t.LanguageCode == "en"), token), courses.Concat(categories).Concat(lessons).OrderByDescending(x => x.DateUtc).Take(5).ToList());
    }
}

