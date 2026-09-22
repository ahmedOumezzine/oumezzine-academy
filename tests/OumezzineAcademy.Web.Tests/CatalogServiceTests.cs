using Xunit;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Tests;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task Maps_home_search_and_categories()
    {
        var query = new CatalogQueries();
        var service = new CourseCatalogService(query, new FixedLanguage("en"));
        var home = await service.GetHomeAsync();
        var search = await service.SearchCoursesAsync("q", null, StudyLevel.Beginner, null, "list", 2, 10);
        var categories = await service.GetCategoriesAsync();

        Assert.Equal(1, home.CourseCount);
        Assert.Equal("list", search.View);
        Assert.Equal("en", query.LastLanguage);
        Assert.Single(categories);
    }

    [Fact]
    public async Task Maps_details_and_returns_null_for_missing_items()
    {
        var service = new CourseCatalogService(new CatalogQueries(), new FixedLanguage("fr"));
        var course = await service.GetCourseAsync("course");
        var category = await service.GetCategoryAsync("category");
        var missing = await service.GetCourseAsync("missing");

        Assert.Equal("Course", course!.Title);
        Assert.Single(course.Contents);
        Assert.Equal("Category", category!.Title);
        Assert.Null(missing);
    }

    private sealed class FixedLanguage(string code) : ICurrentLanguageService { public string LanguageCode => code; }

    private sealed class CatalogQueries : ICourseCatalogQueries
    {
        public string? LastLanguage { get; private set; }
        private static readonly Guid Id = Guid.NewGuid();
        private static CourseSummaryDto Summary => new(Id, "Course", "course", "Summary", null, StudyLevel.Beginner, "Category", "category", 1, 1, 30, DateTime.UtcNow);
        public Task<HomeSummaryDto> GetHomeAsync(string languageCode, CancellationToken cancellationToken = default) { LastLanguage = languageCode; return Task.FromResult(new HomeSummaryDto(1, 1, 1, 1, [Summary], [new("Category", "category", null, 1)], [])); }
        public Task<PagedResult<CourseSummaryDto>> SearchCoursesAsync(CourseSearchCriteria criteria, CancellationToken cancellationToken = default) { LastLanguage = criteria.LanguageCode; return Task.FromResult(new PagedResult<CourseSummaryDto>([Summary], criteria.Page, criteria.PageSize, 1)); }
        public Task<CourseDetailsDto?> GetCourseAsync(string slug, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult(slug == "missing" ? null : new CourseDetailsDto(Summary, "Overview", null, null, null, null, null, null, [new("Chapter", "chapter", null, 1, [new("Lesson", "lesson", null, 5, 1)], [new("Quiz", "quiz", null, 1, 1)])], [], "Category summary"));
        public Task<LessonDetailsDto?> GetLessonAsync(string slug, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult<LessonDetailsDto?>(null);
        public Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(string languageCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CategorySummaryDto>>([new("Category", "category", null, 1)]);
        public Task<CategoryDetailsDto?> GetCategoryAsync(string slug, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult<CategoryDetailsDto?>(new(new("Category", slug, null, 1), null, null, [Summary]));
    }
}
