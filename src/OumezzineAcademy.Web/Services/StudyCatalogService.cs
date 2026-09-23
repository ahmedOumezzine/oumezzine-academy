using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Models.Catalog;

namespace OumezzineAcademy.Web.Services;

public interface ICourseCatalogService
{
    Task<HomeViewModel> GetHomeAsync();

    Task<CourseSearchViewModel> SearchCoursesAsync(
        string? q,
        string? category,
        StudyLevel? level,
        string? sort,
        string? view,
        int page,
        int pageSize);

    Task<CourseDetailViewModel?> GetCourseAsync(string slug);

    Task<LessonDetailViewModel?> GetLessonAsync(string slug);

    Task<IReadOnlyList<CategoryCardViewModel>> GetCategoriesAsync();

    Task<CategoryDetailViewModel?> GetCategoryAsync(string slug);
}

public interface ILearningPathCatalogService
{
    Task<LearningPathListViewModel> GetPathsAsync(
        string? category,
        StudyLevel? level);

    Task<LearningPathDetailViewModel?> GetPathAsync(string slug);
}

public sealed class CourseCatalogService(
    ICourseCatalogQueries queries,
    ICurrentLanguageService language) : ICourseCatalogService
{
    private string LanguageCode => language.LanguageCode;

    private static CourseCardViewModel ToCard(CourseSummaryDto course)
        => new()
        {
            Id = course.Id,
            Title = course.Title,
            Slug = course.Slug,
            Summary = course.Summary,
            Thumbnail = course.Thumbnail,
            Level = course.Level,
            CategoryTitle = course.CategoryTitle,
            CategorySlug = course.CategorySlug,
            LessonCount = course.LessonCount,
            QuizCount = course.QuizCount,
            DurationMinutes = course.DurationMinutes,
            CreatedOnUtc = course.CreatedOnUtc
        };

    private static CategoryCardViewModel ToCategoryCard(CategorySummaryDto category)
        => new()
        {
            Title = category.Title,
            Slug = category.Slug,
            Summary = category.Summary,
            CourseCount = category.CourseCount
        };

    private static LearningPathCardViewModel ToPathCard(LearningPathSummaryDto path)
        => new()
        {
            Title = path.Title,
            Slug = path.Slug,
            Summary = path.Summary,
            Thumbnail = path.Thumbnail,
            Level = path.Level,
            CategoryTitle = path.CategoryTitle,
            CategorySlug = path.CategorySlug,
            CategorySummary = path.CategorySummary,
            CourseCount = path.CourseCount
        };

    private static CourseLessonViewModel ToLesson(LessonSummaryDto lesson)
        => new()
        {
            Title = lesson.Title,
            Slug = lesson.Slug,
            Summary = lesson.Summary,
            Description = lesson.Description,
            VideoUrl = lesson.VideoUrl,
            DurationMinutes = lesson.DurationMinutes,
            Order = lesson.Order
        };

    private static CourseQuizCardViewModel ToQuiz(QuizSummaryDto quiz)
        => new()
        {
            Title = quiz.Title,
            Slug = quiz.Slug,
            Summary = quiz.Summary,
            QuestionCount = quiz.QuestionCount,
            Order = quiz.Order
        };

    public async Task<HomeViewModel> GetHomeAsync()
    {
        var home = await queries.GetHomeAsync(LanguageCode);

        return new()
        {
            CourseCount = home.CourseCount,
            CategoryCount = home.CategoryCount,
            LearningPathCount = home.LearningPathCount,
            LessonCount = home.LessonCount,
            LatestCourses = home.LatestCourses.Select(ToCard).ToList(),
            TopCategories = home.TopCategories.Select(ToCategoryCard).ToList(),
            LatestLearningPaths = home.LatestLearningPaths.Select(ToPathCard).ToList()
        };
    }

    public async Task<CourseSearchViewModel> SearchCoursesAsync(
        string? q,
        string? category,
        StudyLevel? level,
        string? sort,
        string? view,
        int page,
        int pageSize)
    {
        var result = await queries.SearchCoursesAsync(
            new(q, category, level, sort, page, pageSize, LanguageCode));

        return new()
        {
            Q = q,
            Category = category,
            Level = level,
            Sort = sort ?? "recent",
            View = view == "list" ? "list" : "grid",
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            Courses = result.Items.Select(ToCard).ToList()
        };
    }

    public async Task<IReadOnlyList<CategoryCardViewModel>> GetCategoriesAsync()
        => (await queries.GetCategoriesAsync(LanguageCode))
            .Select(ToCategoryCard)
            .ToList();

    public async Task<CategoryDetailViewModel?> GetCategoryAsync(string slug)
    {
        var category = await queries.GetCategoryAsync(slug, LanguageCode);

        if (category is null)
        {
            return null;
        }

        return new()
        {
            Title = category.Category.Title,
            Slug = category.Category.Slug,
            Summary = category.Category.Summary,
            CourseCount = category.Category.CourseCount,
            MetaTitle = category.MetaTitle,
            MetaDescription = category.MetaDescription,
            Courses = category.Courses.Select(ToCard).ToList()
        };
    }

    public async Task<CourseDetailViewModel?> GetCourseAsync(string slug)
    {
        var course = await queries.GetCourseAsync(slug, LanguageCode);

        if (course is null)
        {
            return null;
        }

        return new()
        {
            Id = course.Course.Id,
            Title = course.Course.Title,
            Slug = course.Course.Slug,
            Summary = course.Course.Summary,
            Thumbnail = course.Course.Thumbnail,
            Level = course.Course.Level,
            CategoryTitle = course.Course.CategoryTitle,
            CategorySlug = course.Course.CategorySlug,
            LessonCount = course.Course.LessonCount,
            QuizCount = course.Course.QuizCount,
            DurationMinutes = course.Course.DurationMinutes,
            CreatedOnUtc = course.Course.CreatedOnUtc,
            Overview = course.Overview,
            WhatYouLearn = course.WhatYouLearn,
            Requirements = course.Requirements,
            Audience = course.Audience,
            MetaTitle = course.MetaTitle,
            MetaDescription = course.MetaDescription,
            UpdatedOnUtc = course.UpdatedOnUtc,
            CategorySummary = course.CategorySummary,
            Contents = course.Chapters
                .Select(chapter => new CourseContentViewModel
                {
                    Title = chapter.Title,
                    Slug = chapter.Slug,
                    Summary = chapter.Summary,
                    Order = chapter.Order,
                    Lessons = chapter.Lessons.Select(ToLesson).ToList(),
                    Quizzes = chapter.Quizzes.Select(ToQuiz).ToList()
                })
                .ToList(),
            RelatedCourses = course.RelatedCourses.Select(ToCard).ToList()
        };
    }

    public async Task<LessonDetailViewModel?> GetLessonAsync(string slug)
    {
        var lesson = await queries.GetLessonAsync(slug, LanguageCode);

        if (lesson is null)
        {
            return null;
        }

        return new()
        {
            Title = lesson.Lesson.Title,
            Slug = lesson.Lesson.Slug,
            Summary = lesson.Lesson.Summary,
            Description = lesson.Description,
            VideoUrl = lesson.VideoUrl,
            DocumentUrl = lesson.DocumentUrl,
            DurationMinutes = lesson.Lesson.DurationMinutes,
            Order = lesson.Lesson.Order,
            MetaTitle = lesson.MetaTitle,
            MetaDescription = lesson.MetaDescription,
            CourseTitle = lesson.CourseTitle,
            CourseSlug = lesson.CourseSlug,
            CourseSummary = lesson.CourseSummary,
            CourseThumbnail = lesson.CourseThumbnail,
            CategoryTitle = lesson.CategoryTitle,
            CategorySlug = lesson.CategorySlug,
            ChapterTitle = lesson.ChapterTitle,
            ChapterSummary = lesson.ChapterSummary,
            ChapterLessons = lesson.ChapterLessons.Select(ToLesson).ToList(),
            ChapterQuizzes = lesson.ChapterQuizzes.Select(ToQuiz).ToList()
        };
    }
}

public sealed class LearningPathCatalogService(
    ILearningPathQueries queries,
    ICurrentLanguageService language) : ILearningPathCatalogService
{
    private string LanguageCode => language.LanguageCode;

    private static LearningPathCardViewModel ToCard(LearningPathSummaryDto path)
        => new()
        {
            Title = path.Title,
            Slug = path.Slug,
            Summary = path.Summary,
            Thumbnail = path.Thumbnail,
            Level = path.Level,
            CategoryTitle = path.CategoryTitle,
            CategorySlug = path.CategorySlug,
            CategorySummary = path.CategorySummary,
            CourseCount = path.CourseCount
        };

    public async Task<LearningPathListViewModel> GetPathsAsync(
        string? category,
        StudyLevel? level)
    {
        var paths = await queries.GetPathsAsync(category, level, LanguageCode);

        return new()
        {
            Category = category,
            Level = level,
            Paths = paths.Select(ToCard).ToList()
        };
    }

    public async Task<LearningPathDetailViewModel?> GetPathAsync(string slug)
    {
        var path = await queries.GetPathAsync(slug, LanguageCode);

        if (path is null)
        {
            return null;
        }

        return new()
        {
            Title = path.LearningPath.Title,
            Slug = path.LearningPath.Slug,
            Summary = path.LearningPath.Summary,
            Thumbnail = path.LearningPath.Thumbnail,
            Level = path.LearningPath.Level,
            CategoryTitle = path.LearningPath.CategoryTitle,
            CategorySlug = path.LearningPath.CategorySlug,
            CategorySummary = path.LearningPath.CategorySummary,
            CourseCount = path.LearningPath.CourseCount,
            MetaTitle = path.MetaTitle,
            MetaDescription = path.MetaDescription,
            Courses = path.Courses
                .Select(course => new LearningPathStepViewModel
                {
                    CourseId = course.CourseId,
                    CourseTitle = course.CourseTitle,
                    CourseSlug = course.CourseSlug,
                    Summary = course.Summary,
                    ThumbnailUrl = course.Thumbnail,
                    LessonCount = course.LessonCount,
                    QuizCount = course.QuizCount,
                    DurationMinutes = course.DurationMinutes,
                    Order = course.Order
                })
                .ToList()
        };
    }
}
