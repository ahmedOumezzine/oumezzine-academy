using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfLearningPathQueries(IRepository repository) : ILearningPathQueries
{
    public async Task<IReadOnlyList<LearningPathSummaryDto>> GetPathsAsync(
        string? category,
        StudyLevel? level,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var paths = await repository.GetListAsync<LearningPath>(
            query => query
                .Include(path => path.Translations)
                .Include(path => path.LearningPathCategory)
                .ThenInclude(pathCategory => pathCategory.Translations)
                .Include(path => path.LearningPathCourses)
                .ThenInclude(link => link.Course)
                .ThenInclude(course => course.Translations),
            cancellationToken);

        return paths
            .Where(path => path.Status == StudyStatus.Published
                && path.Translations.Any(translation =>
                    translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published)
                && (string.IsNullOrWhiteSpace(category)
                    || path.LearningPathCategory.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published
                        && translation.Slug == category))
                && (!level.HasValue || level.Value == StudyLevel.All || path.Level == level.Value))
            .OrderByDescending(path => path.CreatedOnUtc)
            .Select(path => new LearningPathSummaryDto(
                PathTranslation(path.Translations, languageCode)?.Title!,
                PathTranslation(path.Translations, languageCode)?.Slug!,
                PathTranslation(path.Translations, languageCode)?.Summary,
                path.Thumbnail,
                path.Level,
                CategoryTranslation(path.LearningPathCategory.Translations, languageCode)?.Title!,
                CategoryTranslation(path.LearningPathCategory.Translations, languageCode)?.Slug!,
                CategoryTranslation(path.LearningPathCategory.Translations, languageCode)?.Summary,
                path.LearningPathCourses.Count(link =>
                    link.Course.Status == StudyStatus.Published
                    && link.Course.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published))))
            .ToList();
    }

    public async Task<LearningPathDetailsDto?> GetPathAsync(
        string slug,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var paths = await repository.GetListAsync<LearningPath>(
            query => query
                .Include(path => path.Translations)
                .Include(path => path.LearningPathCategory)
                .ThenInclude(pathCategory => pathCategory.Translations)
                .Include(path => path.LearningPathCourses)
                .ThenInclude(link => link.Course)
                .ThenInclude(course => course.Translations)
                .Include(path => path.LearningPathCourses)
                .ThenInclude(link => link.Course)
                .ThenInclude(course => course.CourseContents)
                .ThenInclude(content => content.CourseLessons)
                .ThenInclude(lesson => lesson.Translations)
                .Include(path => path.LearningPathCourses)
                .ThenInclude(link => link.Course)
                .ThenInclude(course => course.CourseContents)
                .ThenInclude(content => content.CourseQuizzes)
                .ThenInclude(quiz => quiz.Translations),
            cancellationToken);

        var path = paths.FirstOrDefault(item =>
            item.Status == StudyStatus.Published
            && PathTranslation(item.Translations, languageCode) is { } translation
            && translation.Slug == slug);

        if (path is null)
        {
            return null;
        }

        var pathTranslation = PathTranslation(path.Translations, languageCode)!;
        var categoryTranslation = CategoryTranslation(path.LearningPathCategory.Translations, languageCode);
        var courses = path.LearningPathCourses
            .Where(link => link.Course.Status == StudyStatus.Published
                && link.Course.Translations.Any(translation =>
                    translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published))
            .OrderBy(link => link.Order)
            .Select(link =>
            {
                var courseTranslation = CourseTranslation(link.Course.Translations, languageCode)!;
                var contents = link.Course.CourseContents ?? new List<CourseContent>();
                var lessons = contents
                    .SelectMany(content => content.CourseLessons)
                    .Count(lesson => lesson.Status == StudyStatus.Published
                        && lesson.Translations.Any(translation =>
                            translation.LanguageCode == languageCode
                            && translation.PublicationStatus == StudyStatus.Published));
                var quizzes = contents
                    .SelectMany(content => content.CourseQuizzes)
                    .Count(quiz => quiz.Status == StudyStatus.Published
                        && quiz.Translations.Any(translation =>
                            translation.LanguageCode == languageCode
                            && translation.PublicationStatus == StudyStatus.Published));
                var duration = contents
                    .SelectMany(content => content.CourseLessons)
                    .Where(lesson => lesson.Status == StudyStatus.Published
                        && lesson.Translations.Any(translation =>
                            translation.LanguageCode == languageCode
                            && translation.PublicationStatus == StudyStatus.Published))
                    .Sum(lesson => lesson.DurationMinutes ?? 0);

                return new LearningPathCourseDto(
                    link.Course.Id,
                    courseTranslation.Title!,
                    courseTranslation.Slug!,
                    courseTranslation.Summary,
                    link.Course.Thumbnail,
                    lessons,
                    quizzes,
                    duration,
                    link.Order);
            })
            .ToList();

        var summary = new LearningPathSummaryDto(
            pathTranslation.Title!,
            pathTranslation.Slug!,
            pathTranslation.Summary,
            path.Thumbnail,
            path.Level,
            categoryTranslation?.Title ?? string.Empty,
            categoryTranslation?.Slug ?? string.Empty,
            categoryTranslation?.Summary,
            courses.Count);

        return new(
            summary,
            pathTranslation.MetaTitle,
            pathTranslation.MetaDescription,
            courses);
    }

    private static LearningPathTranslation? PathTranslation(
        IEnumerable<LearningPathTranslation> translations,
        string languageCode)
        => translations.FirstOrDefault(translation =>
            translation.LanguageCode == languageCode
            && translation.PublicationStatus == StudyStatus.Published);

    private static LearningPathCategoryTranslation? CategoryTranslation(
        IEnumerable<LearningPathCategoryTranslation> translations,
        string languageCode)
        => translations.FirstOrDefault(translation =>
            translation.LanguageCode == languageCode
            && translation.PublicationStatus == StudyStatus.Published);

    private static CourseTranslation? CourseTranslation(
        IEnumerable<CourseTranslation> translations,
        string languageCode)
        => translations.FirstOrDefault(translation =>
            translation.LanguageCode == languageCode
            && translation.PublicationStatus == StudyStatus.Published);
}
