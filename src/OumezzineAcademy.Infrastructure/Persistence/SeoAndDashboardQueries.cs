using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfSitemapQueries(IRepository repository) : ISitemapQueries
{
    public async Task<SitemapSlugs> GetSlugsAsync(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var courses = await repository.GetListAsync<Course>(
            query => query.Include(course => course.Translations),
            cancellationToken);
        var categories = await repository.GetListAsync<CourseCategory>(
            query => query.Include(category => category.Translations),
            cancellationToken);
        var paths = await repository.GetListAsync<LearningPath>(
            query => query.Include(path => path.Translations),
            cancellationToken);
        var lessons = await repository.GetListAsync<CourseLesson>(
            query => query
                .Include(lesson => lesson.Translations)
                .Include(lesson => lesson.CourseContent)
                .ThenInclude(content => content.Course),
            cancellationToken);

        return new(
            courses
                .Where(course => course.Status == StudyStatus.Published)
                .SelectMany(course => course.Translations
                    .Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
                    .Select(translation => translation.Slug))
                .ToList(),
            categories
                .Where(category => category.Status == StudyStatus.Published)
                .SelectMany(category => category.Translations
                    .Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
                    .Select(translation => translation.Slug))
                .ToList(),
            paths
                .Where(path => path.Status == StudyStatus.Published)
                .SelectMany(path => path.Translations
                    .Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
                    .Select(translation => translation.Slug))
                .ToList(),
            lessons
                .Where(lesson => lesson.Status == StudyStatus.Published
                    && lesson.CourseContent.Course.Status == StudyStatus.Published)
                .SelectMany(lesson => lesson.Translations
                    .Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
                    .Select(translation => translation.Slug))
                .ToList());
    }
}

public sealed class EfDashboardQueries(
    IRepository repository) : IDashboardQueries
{
    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var courseEntities = await repository.GetListAsync<Course>(
            query => query.Include(course => course.Translations),
            cancellationToken);
        var courses = courseEntities.Select(course => new DashboardRecentItem(
                "Cours",
                course.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? course.Title,
                "modifié",
                course.LastModifiedOnUtc ?? course.CreatedOnUtc,
                course.Id,
                null,
                "book"))
            .Take(5)
            .Take(5).ToList();

        var categoryEntities = await repository.GetListAsync<CourseCategory>(
            query => query.Include(category => category.Translations),
            cancellationToken);
        var categories = categoryEntities.Select(category => new DashboardRecentItem(
                "Catégorie",
                category.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? category.Title,
                "modifiée",
                category.LastModifiedOnUtc ?? category.CreatedOnUtc,
                null,
                null,
                "folder2"))
            .Take(5)
            .Take(5).ToList();

        var lessonEntities = await repository.GetListAsync<CourseLesson>(
            query => query
                .Include(lesson => lesson.Translations)
                .Include(lesson => lesson.CourseContent),
            cancellationToken);
        var lessons = lessonEntities.Select(lesson => new DashboardRecentItem(
                "Leçon",
                lesson.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? lesson.Title,
                "modifiée",
                lesson.LastModifiedOnUtc ?? lesson.CreatedOnUtc,
                lesson.CourseContent.CourseId,
                lesson.CourseContentId,
                "mortarboard"))
            .Take(5)
            .Take(5).ToList();

        return new(
            await repository.CountAsync<Course>(cancellationToken),
            await repository.CountAsync<CourseCategory>(cancellationToken),
            await repository.CountAsync<Course>(
                course => course.Translations.Any(translation =>
                    translation.LanguageCode == "fr"
                    && translation.PublicationStatus == StudyStatus.Published),
                cancellationToken),
            await repository.CountAsync<Course>(
                course => course.Translations.Any(translation =>
                    translation.LanguageCode == "en"
                    && translation.PublicationStatus == StudyStatus.Published),
                cancellationToken),
            await repository.CountAsync<Course>(
                course => course.Translations.Any(translation =>
                    translation.LanguageCode == "en"
                    && translation.PublicationStatus == StudyStatus.Draft),
                cancellationToken),
            await repository.CountAsync<Course>(
                course => !course.Translations.Any(translation =>
                    translation.LanguageCode == "en"),
                cancellationToken),
            await repository.CountAsync<CourseLesson>(cancellationToken),
            await repository.CountAsync<CourseQuiz>(cancellationToken),
            await repository.CountAsync<LearningPath>(cancellationToken),
            await repository.CountAsync<Course>(
                course => course.Thumbnail == null || course.Thumbnail == "",
                cancellationToken),
            await repository.CountAsync<CourseLesson>(
                lesson => lesson.Status == StudyStatus.Draft,
                cancellationToken),
            await repository.CountAsync<CourseLesson>(
                lesson => !lesson.Translations.Any(translation =>
                    translation.LanguageCode == "en"),
                cancellationToken),
            courses
                .Concat(categories)
                .Concat(lessons)
                .OrderByDescending(item => item.DateUtc)
                .Take(5)
                .ToList());
    }
}
