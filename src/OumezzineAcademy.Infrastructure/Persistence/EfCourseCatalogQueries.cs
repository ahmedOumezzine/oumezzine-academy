using AhmedOumezzine.EFCore.Repository.Interface;
using AhmedOumezzine.EFCore.Repository.Specification;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfCourseCatalogQueries(IRepository repository) : ICourseCatalogQueries
{
    private static NotSupportedException Pending() => new("Catalog detail projection is implemented in the next catalog step.");

    public async Task<HomeSummaryDto> GetHomeAsync(string languageCode, CancellationToken token = default)
    {
        var courseSpecification = new Specification<Course>
        {
            Conditions =
            {
                course => course.Status == StudyStatus.Published
                    && course.Translations.Any(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            },
            OrderBy = query => query.OrderByDescending(course => course.CreatedOnUtc)
        };
        var courses = (await repository.GetListAsync<Course, CourseSummaryDto>(
            courseSpecification,
            course => new CourseSummaryDto(
                course.Id,
                course.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                course.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                course.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                course.Thumbnail,
                course.Level,
                course.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                course.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                course.CourseContents.SelectMany(content => content.CourseLessons).Count(lesson => lesson.Status == StudyStatus.Published),
                course.CourseContents.SelectMany(content => content.CourseQuizzes).Count(quiz => quiz.Status == StudyStatus.Published),
                course.CourseContents.SelectMany(content => content.CourseLessons).Where(lesson => lesson.Status == StudyStatus.Published).Sum(lesson => lesson.DurationMinutes ?? 0),
                course.CreatedOnUtc),
            token)).Take(3).ToList();
        var categories = await GetCategoriesAsync(languageCode, token);
        var pathSpecification = new Specification<LearningPath>
        {
            Conditions =
            {
                path => path.Status == StudyStatus.Published
                    && path.Translations.Any(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            },
            OrderBy = query => query.OrderByDescending(path => path.CreatedOnUtc)
        };
        var paths = (await repository.GetListAsync<LearningPath, LearningPathSummaryDto>(
            pathSpecification,
            path => new LearningPathSummaryDto(
                path.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                path.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                path.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                path.Thumbnail,
                path.Level,
                path.LearningPathCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                path.LearningPathCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                path.LearningPathCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                path.LearningPathCourses.Count),
            token)).Take(4).ToList();
        var courseCount = await repository.CountAsync<Course>(course => course.Status == StudyStatus.Published
            && course.Translations.Any(translation => translation.LanguageCode == languageCode
                && translation.PublicationStatus == StudyStatus.Published), token);
        var categoryCount = await repository.CountAsync<CourseCategory>(category => category.Status == StudyStatus.Published
            && category.Translations.Any(translation => translation.LanguageCode == languageCode
                && translation.PublicationStatus == StudyStatus.Published), token);
        var pathCount = await repository.CountAsync<LearningPath>(path => path.Status == StudyStatus.Published
            && path.Translations.Any(translation => translation.LanguageCode == languageCode
                && translation.PublicationStatus == StudyStatus.Published), token);
        var lessonCount = await repository.CountAsync<CourseLesson>(lesson => lesson.Status == StudyStatus.Published
            && lesson.Translations.Any(translation => translation.LanguageCode == languageCode
                && translation.PublicationStatus == StudyStatus.Published), token);
        return new(courseCount, categoryCount, pathCount, lessonCount, courses, categories, paths);
    }

    public async Task<CourseDetailsDto?> GetCourseAsync(string slug, string languageCode, CancellationToken token = default)
    {
        var courseSpecification = new Specification<Course>
        {
            Conditions =
            {
                course => course.Status == StudyStatus.Published
                    && course.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published
                        && translation.Slug == slug)
            }
        };
        var course = (await repository.GetListAsync<Course, CourseHeaderRow>(
            courseSpecification,
            entity => new CourseHeaderRow(
                entity.Id,
                entity.CourseCategoryId,
                new CourseSummaryDto(
                    entity.Id,
                    entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                    entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                    entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                    entity.Thumbnail,
                    entity.Level,
                    entity.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                    entity.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                    entity.CourseContents.SelectMany(content => content.CourseLessons).Count(lesson => lesson.Status == StudyStatus.Published),
                    entity.CourseContents.SelectMany(content => content.CourseQuizzes).Count(quiz => quiz.Status == StudyStatus.Published),
                    entity.CourseContents.SelectMany(content => content.CourseLessons).Where(lesson => lesson.Status == StudyStatus.Published).Sum(lesson => lesson.DurationMinutes ?? 0),
                    entity.CreatedOnUtc),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Overview).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.WhatYouLearn).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Requirements).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Audience).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.MetaTitle).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.MetaDescription).FirstOrDefault(),
                entity.LastModifiedOnUtc,
                entity.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault()),
            token)).FirstOrDefault();
        if (course is null) return null;

        var chapterSpecification = new Specification<CourseContent>
        {
            Conditions =
            {
                chapter => chapter.CourseId == course.Id
                    && chapter.Status == StudyStatus.Published
                    && chapter.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            },
            OrderBy = query => query.OrderBy(chapter => chapter.Order)
        };
        var chapters = await repository.GetListAsync<CourseContent, CourseChapterRow>(
            chapterSpecification,
            chapter => new CourseChapterRow(
                chapter.Id,
                chapter.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                chapter.Slug,
                chapter.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                chapter.Order),
            token);
        var chapterIds = chapters.Select(ch => ch.Id).ToList();
        var lessonSpecification = new Specification<CourseLesson>
        {
            Conditions =
            {
                lesson => chapterIds.Contains(lesson.CourseContentId)
                    && lesson.Status == StudyStatus.Published
                    && lesson.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            }
        };
        var lessons = await repository.GetListAsync<CourseLesson, CourseLessonRow>(
            lessonSpecification,
            lesson => new CourseLessonRow(
                lesson.CourseContentId,
                lesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                lesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                lesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                lesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.ContentHtml).FirstOrDefault(),
                lesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.VideoUrl).FirstOrDefault(),
                lesson.DurationMinutes ?? 0,
                lesson.Order),
            token);
        var quizSpecification = new Specification<CourseQuiz>
        {
            Conditions =
            {
                quiz => chapterIds.Contains(quiz.CourseContentId)
                    && quiz.Status == StudyStatus.Published
                    && quiz.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            }
        };
        var quizzes = await repository.GetListAsync<CourseQuiz, CourseQuizRow>(
            quizSpecification,
            quiz => new CourseQuizRow(
                quiz.CourseContentId,
                quiz.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                quiz.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                quiz.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                quiz.Order,
                quiz.QuizQuestions.Count),
            token);
        var nestedChapters = chapters.Select(ch => new CourseChapterDto(ch.Title, ch.Slug, ch.Summary, ch.Order,
            lessons.Where(l => l.CourseContentId == ch.Id).OrderBy(l => l.Order).Select(l => new LessonSummaryDto(l.Title, l.Slug, l.Summary, l.DurationMinutes, l.Order, l.Description, l.VideoUrl)).ToList(),
            quizzes.Where(q => q.CourseContentId == ch.Id).OrderBy(q => q.Order).Select(q => new QuizSummaryDto(q.Title, q.Slug, q.Summary, q.QuestionCount, q.Order)).ToList())).ToList();
        var relatedSpecification = new Specification<Course>
        {
            Conditions =
            {
                relatedCourse => relatedCourse.CourseCategoryId == course.CourseCategoryId
                    && relatedCourse.Id != course.Id
                    && relatedCourse.Status == StudyStatus.Published
                    && relatedCourse.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            },
            OrderBy = query => query.OrderByDescending(relatedCourse => relatedCourse.CreatedOnUtc)
        };
        var related = (await repository.GetListAsync<Course, CourseSummaryDto>(
            relatedSpecification,
            relatedCourse => new CourseSummaryDto(
                relatedCourse.Id,
                relatedCourse.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                relatedCourse.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                relatedCourse.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                relatedCourse.Thumbnail,
                relatedCourse.Level,
                relatedCourse.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                relatedCourse.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                relatedCourse.CourseContents.SelectMany(content => content.CourseLessons).Count(lesson => lesson.Status == StudyStatus.Published),
                relatedCourse.CourseContents.SelectMany(content => content.CourseQuizzes).Count(quiz => quiz.Status == StudyStatus.Published),
                relatedCourse.CourseContents.SelectMany(content => content.CourseLessons).Where(lesson => lesson.Status == StudyStatus.Published).Sum(lesson => lesson.DurationMinutes ?? 0),
                relatedCourse.CreatedOnUtc),
            token)).Take(3).ToList();
        return new CourseDetailsDto(course.Course, course.Overview, course.WhatYouLearn, course.Requirements, course.Audience, course.MetaTitle, course.MetaDescription, course.LastModifiedOnUtc, nestedChapters, related, course.CategorySummary);
    }

    public async Task<LessonDetailsDto?> GetLessonAsync(string slug, string languageCode, CancellationToken token = default)
    {
        var lessonSpecification = new Specification<CourseLesson>
        {
            Conditions =
            {
                lesson => lesson.Status == StudyStatus.Published
                    && lesson.Translations.Any(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published
                        && translation.Slug == slug)
                    && lesson.CourseContent.Status == StudyStatus.Published
                    && lesson.CourseContent.Course.Status == StudyStatus.Published
                    && lesson.CourseContent.Course.Translations.Any(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
                    && lesson.CourseContent.Translations.Any(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            }
        };
        var lesson = (await repository.GetListAsync<CourseLesson, LessonHeaderRow>(
            lessonSpecification,
            entity => new LessonHeaderRow(
                entity.Id,
                entity.CourseContentId,
                entity.CourseContent.CourseId,
                entity.CourseContent.Course.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                entity.CourseContent.Course.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                entity.CourseContent.Course.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault() ?? "",
                entity.CourseContent.Course.Thumbnail,
                entity.CourseContent.Course.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                entity.CourseContent.Course.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                entity.CourseContent.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                entity.CourseContent.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                entity.CourseContent.Order,
                new LessonSummaryDto(
                    entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                    entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                    entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                    entity.DurationMinutes ?? 0,
                    entity.Order,
                    entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.ContentHtml).FirstOrDefault(),
                    entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.VideoUrl).FirstOrDefault()),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.ContentHtml).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.VideoUrl).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.DocumentUrl).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.MetaTitle).FirstOrDefault(),
                entity.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.MetaDescription).FirstOrDefault()),
            token)).FirstOrDefault();
        if (lesson is null) return null;

        var chapterLessonSpecification = new Specification<CourseLesson>
        {
            Conditions =
            {
                chapterLesson => chapterLesson.CourseContentId == lesson.CourseContentId
                    && chapterLesson.Status == StudyStatus.Published
                    && chapterLesson.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            },
            OrderBy = query => query.OrderBy(chapterLesson => chapterLesson.Order)
        };
        var chapterLessons = await repository.GetListAsync<CourseLesson, LessonSummaryDto>(
            chapterLessonSpecification,
            chapterLesson => new LessonSummaryDto(
                chapterLesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                chapterLesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                chapterLesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                chapterLesson.DurationMinutes ?? 0,
                chapterLesson.Order,
                chapterLesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.ContentHtml).FirstOrDefault(),
                chapterLesson.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.VideoUrl).FirstOrDefault()),
            token);
        var chapterQuizSpecification = new Specification<CourseQuiz>
        {
            Conditions =
            {
                chapterQuiz => chapterQuiz.CourseContentId == lesson.CourseContentId
                    && chapterQuiz.Status == StudyStatus.Published
                    && chapterQuiz.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            },
            OrderBy = query => query.OrderBy(chapterQuiz => chapterQuiz.Order)
        };
        var chapterQuizzes = await repository.GetListAsync<CourseQuiz, QuizSummaryDto>(
            chapterQuizSpecification,
            chapterQuiz => new QuizSummaryDto(
                chapterQuiz.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                chapterQuiz.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                chapterQuiz.Translations.Where(translation => translation.LanguageCode == languageCode && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                chapterQuiz.QuizQuestions.Count,
                chapterQuiz.Order),
            token);
        return new LessonDetailsDto(lesson.Lesson, lesson.Description, lesson.VideoUrl, lesson.DocumentUrl, lesson.MetaTitle, lesson.MetaDescription,
            lesson.CourseTitle, lesson.CourseSlug, lesson.CourseSummary, lesson.CourseThumbnail, lesson.CategoryTitle, lesson.CategorySlug,
            lesson.ChapterTitle, lesson.ChapterSummary, chapterLessons, chapterQuizzes);
    }

    public async Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(
        string languageCode,
        CancellationToken token = default)
    {
        var specification = new Specification<CourseCategory>
        {
            Conditions =
            {
                category => category.Status == StudyStatus.Published
                    && category.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            },
            OrderBy = query => query.OrderBy(category => category.Translations
                .Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published)
                .Select(translation => translation.Title)
                .FirstOrDefault())
        };

        return await repository.GetListAsync<CourseCategory, CategorySummaryDto>(
            specification,
            category => new CategorySummaryDto(
                category.Translations
                    .Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
                    .Select(translation => translation.Title)
                    .FirstOrDefault()!,
                category.Translations
                    .Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
                    .Select(translation => translation.Slug)
                    .FirstOrDefault()!,
                category.Translations
                    .Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
                    .Select(translation => translation.Summary)
                    .FirstOrDefault(),
                category.Courses.Count(course => course.Status == StudyStatus.Published
                    && course.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published))),
            token);
    }

    public async Task<CategoryDetailsDto?> GetCategoryAsync(string slug, string languageCode, CancellationToken token = default)
    {
        var categorySpecification = new Specification<CourseCategory>
        {
            Conditions =
            {
                category => category.Status == StudyStatus.Published
                    && category.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published
                        && translation.Slug == slug)
            }
        };

        var categories = await repository.GetListAsync<CourseCategory, CategoryDetailsDto>(
            categorySpecification,
            category => new CategoryDetailsDto(
                new CategorySummaryDto(
                    category.Translations.Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                    category.Translations.Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                    category.Translations.Where(translation => translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                    category.Courses.Count(course => course.Status == StudyStatus.Published
                        && course.Translations.Any(translation => translation.LanguageCode == languageCode
                            && translation.PublicationStatus == StudyStatus.Published))),
                category.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.MetaTitle).FirstOrDefault(),
                category.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.MetaDescription).FirstOrDefault(),
                new List<CourseSummaryDto>()),
            token);
        var category = categories.FirstOrDefault();
        if (category is null) return null;
        var courseSpecification = new Specification<Course>
        {
            Conditions =
            {
                course => course.Status == StudyStatus.Published
                    && course.CourseCategory.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published
                        && translation.Slug == slug)
                    && course.Translations.Any(translation =>
                        translation.LanguageCode == languageCode
                        && translation.PublicationStatus == StudyStatus.Published)
            },
            OrderBy = query => query.OrderBy(course => course.Translations
                .Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published)
                .Select(translation => translation.Title)
                .FirstOrDefault())
        };

        var courses = await repository.GetListAsync<Course, CourseSummaryDto>(
            courseSpecification,
            course => new CourseSummaryDto(
                course.Id,
                course.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                course.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                course.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                course.Thumbnail,
                course.Level,
                course.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                course.CourseCategory.Translations.Where(translation => translation.LanguageCode == languageCode
                    && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                0,
                0,
                0,
                course.CreatedOnUtc),
            token);
        return category with { Courses = courses };
    }

    public async Task<PagedResult<CourseSummaryDto>> SearchCoursesAsync(CourseSearchCriteria criteria, CancellationToken token = default)
    {
        var language = criteria.LanguageCode;
        var specification = new Specification<Course>
        {
            Conditions =
            {
                course => course.Status == StudyStatus.Published
                    && course.Translations.Any(translation =>
                        translation.LanguageCode == language
                        && translation.PublicationStatus == StudyStatus.Published)
            }
        };

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var term = criteria.Query.Trim();
            if (term.Length > 80) term = term[..80];
            specification.Conditions.Add(course => course.Translations.Any(translation =>
                translation.LanguageCode == language
                && translation.PublicationStatus == StudyStatus.Published
                && (translation.Title.Contains(term)
                    || (translation.Summary != null && translation.Summary.Contains(term)))));
        }

        if (!string.IsNullOrWhiteSpace(criteria.CategorySlug))
            specification.Conditions.Add(course => course.CourseCategory.Translations.Any(translation =>
                translation.LanguageCode == language
                && translation.PublicationStatus == StudyStatus.Published
                && translation.Slug == criteria.CategorySlug));

        if (criteria.Level.HasValue && criteria.Level.Value != StudyLevel.All)
            specification.Conditions.Add(course => course.Level == criteria.Level.Value);

        specification.OrderBy = criteria.Sort == "az"
            ? query => query.OrderBy(course => course.Translations
                .Where(translation => translation.LanguageCode == language
                    && translation.PublicationStatus == StudyStatus.Published)
                .Select(translation => translation.Title)
                .FirstOrDefault())
            : criteria.Sort == "level"
                ? query => query.OrderBy(course => course.Level)
                    .ThenBy(course => course.Translations
                        .Where(translation => translation.LanguageCode == language
                            && translation.PublicationStatus == StudyStatus.Published)
                        .Select(translation => translation.Title)
                        .FirstOrDefault())
                : query => query.OrderByDescending(course => course.CreatedOnUtc);

        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Clamp(criteria.PageSize, 6, 24);
        var rows = await repository.GetListAsync<Course, CourseSummaryDto>(
            specification,
            course => new CourseSummaryDto(
                course.Id,
                course.Translations.Where(translation => translation.LanguageCode == language && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                course.Translations.Where(translation => translation.LanguageCode == language && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                course.Translations.Where(translation => translation.LanguageCode == language && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Summary).FirstOrDefault(),
                course.Thumbnail,
                course.Level,
                course.CourseCategory.Translations.Where(translation => translation.LanguageCode == language && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Title).FirstOrDefault()!,
                course.CourseCategory.Translations.Where(translation => translation.LanguageCode == language && translation.PublicationStatus == StudyStatus.Published).Select(translation => translation.Slug).FirstOrDefault()!,
                course.CourseContents.SelectMany(x => x.CourseLessons).Count(x => x.Status == StudyStatus.Published),
                course.CourseContents.SelectMany(x => x.CourseQuizzes).Count(x => x.Status == StudyStatus.Published),
                course.CourseContents.SelectMany(x => x.CourseLessons).Where(x => x.Status == StudyStatus.Published).Sum(x => x.DurationMinutes ?? 0),
                course.CreatedOnUtc),
            token);

        return new PagedResult<CourseSummaryDto>(
            rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page,
            pageSize,
            rows.Count);
    }
}

file sealed record CourseChapterRow(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    int Order);

file sealed record CourseLessonRow(
    Guid CourseContentId,
    string Title,
    string Slug,
    string? Summary,
    string? Description,
    string? VideoUrl,
    int DurationMinutes,
    int Order);

file sealed record CourseQuizRow(
    Guid CourseContentId,
    string Title,
    string Slug,
    string? Summary,
    int Order,
    int QuestionCount);

file sealed record CourseHeaderRow(
    Guid Id,
    Guid CourseCategoryId,
    CourseSummaryDto Course,
    string? Overview,
    string? WhatYouLearn,
    string? Requirements,
    string? Audience,
    string? MetaTitle,
    string? MetaDescription,
    DateTime? LastModifiedOnUtc,
    string? CategorySummary);

file sealed record LessonHeaderRow(
    Guid Id,
    Guid CourseContentId,
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    string CourseSummary,
    string? CourseThumbnail,
    string CategoryTitle,
    string CategorySlug,
    string ChapterTitle,
    string? ChapterSummary,
    int ChapterOrder,
    LessonSummaryDto Lesson,
    string? Description,
    string? VideoUrl,
    string? DocumentUrl,
    string? MetaTitle,
    string? MetaDescription);
