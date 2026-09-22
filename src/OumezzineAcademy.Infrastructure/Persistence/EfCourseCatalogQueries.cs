using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfCourseCatalogQueries(ApplicationDbContext db) : ICourseCatalogQueries
{
    private static NotSupportedException Pending() => new("Catalog detail projection is implemented in the next catalog step.");

    public async Task<HomeSummaryDto> GetHomeAsync(string languageCode, CancellationToken token = default)
    {
        var courses = await db.StudyCourses.AsNoTracking().Where(c => c.Status == StudyStatus.Published && c.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).OrderByDescending(c => c.CreatedOnUtc).Take(3).Select(c => new CourseSummaryDto(c.Id, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), c.Thumbnail, c.Level, c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, c.CourseContents.SelectMany(x => x.CourseLessons).Count(x => x.Status == StudyStatus.Published), c.CourseContents.SelectMany(x => x.CourseQuizzes).Count(x => x.Status == StudyStatus.Published), c.CourseContents.SelectMany(x => x.CourseLessons).Where(x => x.Status == StudyStatus.Published).Sum(x => x.DurationMinutes ?? 0), c.CreatedOnUtc)).ToListAsync(token);
        var categories = await GetCategoriesAsync(languageCode, token);
        var paths = await db.StudyLearningPaths.AsNoTracking().Where(p => p.Status == StudyStatus.Published && p.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).OrderByDescending(p => p.CreatedOnUtc).Take(4).Select(p => new LearningPathSummaryDto(p.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, p.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, p.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), p.Thumbnail, p.Level, p.LearningPathCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, p.LearningPathCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, p.LearningPathCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), p.LearningPathCourses.Count())).ToListAsync(token);
        return new(await db.StudyCourses.CountAsync(c => c.Status == StudyStatus.Published && c.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published), token), await db.StudyCourseCategories.CountAsync(c => c.Status == StudyStatus.Published && c.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published), token), await db.StudyLearningPaths.CountAsync(p => p.Status == StudyStatus.Published && p.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published), token), await db.StudyCourseLessons.CountAsync(l => l.Status == StudyStatus.Published && l.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published), token), courses, categories, paths);
    }

    public async Task<CourseDetailsDto?> GetCourseAsync(string slug, string languageCode, CancellationToken token = default)
    {
        var course = await db.StudyCourses.AsNoTracking()
            .Where(c => c.Status == StudyStatus.Published && c.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published && t.Slug == slug))
            .Select(c => new
            {
                c.Id,
                c.CourseCategoryId,
                Course = new CourseSummaryDto(c.Id,
                    c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                    c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                    c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), c.Thumbnail, c.Level,
                    c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                    c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                    c.CourseContents.SelectMany(x => x.CourseLessons).Count(x => x.Status == StudyStatus.Published),
                    c.CourseContents.SelectMany(x => x.CourseQuizzes).Count(x => x.Status == StudyStatus.Published),
                    c.CourseContents.SelectMany(x => x.CourseLessons).Where(x => x.Status == StudyStatus.Published).Sum(x => x.DurationMinutes ?? 0), c.CreatedOnUtc),
                Overview = c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Overview).FirstOrDefault(),
                WhatYouLearn = c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.WhatYouLearn).FirstOrDefault(),
                Requirements = c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Requirements).FirstOrDefault(),
                Audience = c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Audience).FirstOrDefault(),
                MetaTitle = c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.MetaTitle).FirstOrDefault(),
                MetaDescription = c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.MetaDescription).FirstOrDefault(),
                c.LastModifiedOnUtc,
                CategorySummary = c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault()
            }).FirstOrDefaultAsync(token);
        if (course is null) return null;

        var chapters = await db.StudyCourseContents.AsNoTracking()
            .Where(ch => ch.CourseId == course.Id && ch.Status == StudyStatus.Published && ch.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published))
            .OrderBy(ch => ch.Order)
            .Select(ch => new { ch.Id, Title = ch.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, Slug = ch.Slug, Summary = ch.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), ch.Order })
            .ToListAsync(token);
        var chapterIds = chapters.Select(ch => ch.Id).ToList();
        var lessons = await db.StudyCourseLessons.AsNoTracking().Where(l => chapterIds.Contains(l.CourseContentId) && l.Status == StudyStatus.Published && l.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).Select(l => new { l.CourseContentId, Title = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, Slug = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, Summary = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), Description = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.ContentHtml).FirstOrDefault(), VideoUrl = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.VideoUrl).FirstOrDefault(), DurationMinutes = l.DurationMinutes ?? 0, l.Order }).ToListAsync(token);
        var quizzes = await db.StudyCourseQuizzes.AsNoTracking().Where(q => chapterIds.Contains(q.CourseContentId) && q.Status == StudyStatus.Published && q.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).Select(q => new { q.CourseContentId, Title = q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, Slug = q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, Summary = q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), q.Order, QuestionCount = q.QuizQuestions.Count }).ToListAsync(token);
        var nestedChapters = chapters.Select(ch => new CourseChapterDto(ch.Title, ch.Slug, ch.Summary, ch.Order,
            lessons.Where(l => l.CourseContentId == ch.Id).OrderBy(l => l.Order).Select(l => new LessonSummaryDto(l.Title, l.Slug, l.Summary, l.DurationMinutes, l.Order, l.Description, l.VideoUrl)).ToList(),
            quizzes.Where(q => q.CourseContentId == ch.Id).OrderBy(q => q.Order).Select(q => new QuizSummaryDto(q.Title, q.Slug, q.Summary, q.QuestionCount, q.Order)).ToList())).ToList();
        var related = await db.StudyCourses.AsNoTracking().Where(c => c.CourseCategoryId == course.CourseCategoryId && c.Id != course.Id && c.Status == StudyStatus.Published && c.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).OrderByDescending(c => c.CreatedOnUtc).Take(3).Select(c => new CourseSummaryDto(c.Id, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), c.Thumbnail, c.Level, c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, c.CourseContents.SelectMany(x => x.CourseLessons).Count(x => x.Status == StudyStatus.Published), c.CourseContents.SelectMany(x => x.CourseQuizzes).Count(x => x.Status == StudyStatus.Published), c.CourseContents.SelectMany(x => x.CourseLessons).Where(x => x.Status == StudyStatus.Published).Sum(x => x.DurationMinutes ?? 0), c.CreatedOnUtc)).ToListAsync(token);
        return new CourseDetailsDto(course.Course, course.Overview, course.WhatYouLearn, course.Requirements, course.Audience, course.MetaTitle, course.MetaDescription, course.LastModifiedOnUtc, nestedChapters, related, course.CategorySummary);
    }

    public async Task<LessonDetailsDto?> GetLessonAsync(string slug, string languageCode, CancellationToken token = default)
    {
        var lesson = await db.StudyCourseLessons.AsNoTracking()
            .Where(l => l.Status == StudyStatus.Published && l.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published && t.Slug == slug)
                && l.CourseContent.Status == StudyStatus.Published
                && l.CourseContent.Course.Status == StudyStatus.Published
                && l.CourseContent.Course.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)
                && l.CourseContent.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published))
            .Select(l => new
            {
                l.Id,
                l.CourseContentId,
                CourseId = l.CourseContent.CourseId,
                CourseTitle = l.CourseContent.Course.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                CourseSlug = l.CourseContent.Course.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                CourseSummary = l.CourseContent.Course.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault() ?? "",
                CourseThumbnail = l.CourseContent.Course.Thumbnail,
                CategoryTitle = l.CourseContent.Course.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                CategorySlug = l.CourseContent.Course.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                ChapterTitle = l.CourseContent.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                ChapterSummary = l.CourseContent.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(),
                ChapterOrder = l.CourseContent.Order,
                Lesson = new LessonSummaryDto(
                    l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                    l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                    l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(),
                    l.DurationMinutes ?? 0, l.Order,
                    l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.ContentHtml).FirstOrDefault(),
                    l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.VideoUrl).FirstOrDefault()),
                Description = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.ContentHtml).FirstOrDefault(),
                VideoUrl = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.VideoUrl).FirstOrDefault(),
                DocumentUrl = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.DocumentUrl).FirstOrDefault(),
                MetaTitle = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.MetaTitle).FirstOrDefault(),
                MetaDescription = l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.MetaDescription).FirstOrDefault()
            }).FirstOrDefaultAsync(token);
        if (lesson is null) return null;

        var chapterLessons = await db.StudyCourseLessons.AsNoTracking()
            .Where(l => l.CourseContentId == lesson.CourseContentId && l.Status == StudyStatus.Published && l.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published))
            .OrderBy(l => l.Order)
            .Select(l => new LessonSummaryDto(
                l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(),
                l.DurationMinutes ?? 0, l.Order,
                l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.ContentHtml).FirstOrDefault(),
                l.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.VideoUrl).FirstOrDefault()))
            .ToListAsync(token);
        var chapterQuizzes = await db.StudyCourseQuizzes.AsNoTracking()
            .Where(q => q.CourseContentId == lesson.CourseContentId && q.Status == StudyStatus.Published && q.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published))
            .OrderBy(q => q.Order)
            .Select(q => new QuizSummaryDto(
                q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(),
                q.QuizQuestions.Count, q.Order))
            .ToListAsync(token);
        return new LessonDetailsDto(lesson.Lesson, lesson.Description, lesson.VideoUrl, lesson.DocumentUrl, lesson.MetaTitle, lesson.MetaDescription,
            lesson.CourseTitle, lesson.CourseSlug, lesson.CourseSummary, lesson.CourseThumbnail, lesson.CategoryTitle, lesson.CategorySlug,
            lesson.ChapterTitle, lesson.ChapterSummary, chapterLessons, chapterQuizzes);
    }

    public async Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(string languageCode, CancellationToken token = default) => await db.StudyCourseCategories.AsNoTracking().Where(c => c.Status == StudyStatus.Published && c.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).OrderBy(c => c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()).Select(c => new CategorySummaryDto(c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), c.Courses.Count(x => x.Status == StudyStatus.Published && x.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)))).ToListAsync(token);

    public async Task<CategoryDetailsDto?> GetCategoryAsync(string slug, string languageCode, CancellationToken token = default)
    {
        var category = await db.StudyCourseCategories.AsNoTracking().Where(c => c.Status == StudyStatus.Published && c.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published && t.Slug == slug)).Select(c => new CategoryDetailsDto(new CategorySummaryDto(c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), c.Courses.Count(x => x.Status == StudyStatus.Published && x.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published))), c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.MetaTitle).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.MetaDescription).FirstOrDefault(), new List<CourseSummaryDto>())).FirstOrDefaultAsync(token);
        if (category is null) return null;
        var courses = await db.StudyCourses.AsNoTracking().Where(c => c.Status == StudyStatus.Published && c.CourseCategory.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published && t.Slug == slug) && c.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).OrderBy(c => c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()).Select(c => new CourseSummaryDto(c.Id, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, c.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(), c.Thumbnail, c.Level, c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!, c.CourseCategory.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!, 0, 0, 0, c.CreatedOnUtc)).ToListAsync(token);
        return category with { Courses = courses };
    }

    public async Task<PagedResult<CourseSummaryDto>> SearchCoursesAsync(CourseSearchCriteria criteria, CancellationToken token = default)
    {
        var language = criteria.LanguageCode;
        var query = db.StudyCourses.AsNoTracking()
            .Where(course => course.Status == StudyStatus.Published && course.Translations.Any(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published));

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var term = criteria.Query.Trim();
            if (term.Length > 80) term = term[..80];
            query = query.Where(course => course.Translations.Any(t =>
                t.LanguageCode == language &&
                t.PublicationStatus == StudyStatus.Published &&
                (t.Title.Contains(term) || (t.Summary != null && t.Summary.Contains(term)))));
        }

        if (!string.IsNullOrWhiteSpace(criteria.CategorySlug))
            query = query.Where(course => course.CourseCategory.Translations.Any(t =>
                t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published && t.Slug == criteria.CategorySlug));

        if (criteria.Level.HasValue && criteria.Level.Value != StudyLevel.All)
            query = query.Where(course => course.Level == criteria.Level.Value);

        var ordered = criteria.Sort == "az"
            ? query.OrderBy(course => course.Translations.Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault())
            : criteria.Sort == "level"
                ? query.OrderBy(course => course.Level).ThenBy(course => course.Translations.Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault())
                : query.OrderByDescending(course => course.CreatedOnUtc);

        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Clamp(criteria.PageSize, 6, 24);
        var total = await ordered.CountAsync(token);
        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(course => new CourseSummaryDto(
                course.Id,
                course.Translations.Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                course.Translations.Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                course.Translations.Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(),
                course.Thumbnail,
                course.Level,
                course.CourseCategory.Translations.Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                course.CourseCategory.Translations.Where(t => t.LanguageCode == language && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                course.CourseContents.SelectMany(x => x.CourseLessons).Count(x => x.Status == StudyStatus.Published),
                course.CourseContents.SelectMany(x => x.CourseQuizzes).Count(x => x.Status == StudyStatus.Published),
                course.CourseContents.SelectMany(x => x.CourseLessons).Where(x => x.Status == StudyStatus.Published).Sum(x => x.DurationMinutes ?? 0),
                course.CreatedOnUtc))
            .ToListAsync(token);

        return new PagedResult<CourseSummaryDto>(items, page, pageSize, total);
    }
}