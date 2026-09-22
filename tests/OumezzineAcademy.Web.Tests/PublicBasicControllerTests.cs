using Microsoft.AspNetCore.Mvc;
using OumezzineAcademy.Controllers;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Web.Services;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class PublicBasicControllerTests
{
    [Fact]
    public async Task Home_index_returns_catalog_model_and_about_returns_view()
    {
        var fake = new CatalogFake { Home = new HomeViewModel { CourseCount = 3 } };
        var controller = new HomeController(fake);

        var index = Assert.IsType<ViewResult>(await controller.Index());
        var about = Assert.IsType<ViewResult>(controller.About());

        Assert.Same(fake.Home, index.Model);
        Assert.Same(fake.Home, about.Model);
    }

    [Fact]
    public async Task Course_index_forwards_all_search_arguments()
    {
        var fake = new CatalogFake { Search = new CourseSearchViewModel { TotalItems = 4 } };
        var result = Assert.IsType<ViewResult>(await new CourseController(fake).Index("asp", "web", StudyLevel.Advanced, "az", "list", 2, 12));

        Assert.Same(fake.Search, result.Model);
        Assert.Equal(("asp", "web", StudyLevel.Advanced, "az", "list", 2, 12), fake.LastSearch);
    }

    [Fact]
    public async Task Course_details_rejects_empty_slug_and_missing_course()
    {
        var controller = new CourseController(new CatalogFake());

        Assert.IsType<NotFoundResult>(await controller.Details(" "));
        Assert.IsType<NotFoundResult>(await controller.Details("missing"));
    }

    [Fact]
    public async Task Course_details_returns_view_when_course_exists()
    {
        var fake = new CatalogFake { Course = new CourseDetailViewModel { Slug = "course" } };
        var result = Assert.IsType<ViewResult>(await new CourseController(fake).Details("course"));
        Assert.Same(fake.Course, result.Model);
    }

    [Fact]
    public async Task Category_index_and_details_return_expected_results()
    {
        var fake = new CatalogFake { Categories = [new CategoryCardViewModel { Slug = "web" }], Category = new CategoryDetailViewModel { Slug = "web" } };
        var controller = new CategoryController(fake);

        var index = Assert.IsType<ViewResult>(await controller.Index());
        var categories = Assert.IsAssignableFrom<IReadOnlyList<CategoryCardViewModel>>(index.Model);
        Assert.Single(categories);
        var details = Assert.IsType<ViewResult>(await controller.Details("web"));

        Assert.Equal("web", Assert.IsType<CategoryDetailViewModel>(details.Model).Slug);
        Assert.IsType<NotFoundResult>(await controller.Details(null!));
    }

    [Fact]
    public void Visibility_policy_reports_all_publication_failures()
    {
        var result = CourseVisibilityPolicy.Evaluate(StudyStatus.Draft, StudyStatus.Draft, "fr");
        var archived = CourseVisibilityPolicy.Evaluate(StudyStatus.Archived, StudyStatus.Archived, "en");
        var published = CourseVisibilityPolicy.Evaluate(StudyStatus.Published, StudyStatus.Published, "fr");

        Assert.False(result.IsVisible);
        Assert.Equal(2, result.Reasons.Count);
        Assert.Contains("brouillon", result.Reasons[0]);
        Assert.Contains("anglaise", archived.Reasons[1]);
        Assert.True(published.IsVisible);
        Assert.Empty(published.Reasons);
    }

    private sealed class CatalogFake : ICourseCatalogService
    {
        public HomeViewModel Home { get; init; } = new();
        public CourseSearchViewModel Search { get; init; } = new();
        public CourseDetailViewModel? Course { get; init; }
        public CategoryDetailViewModel? Category { get; init; }
        public IReadOnlyList<CategoryCardViewModel> Categories { get; init; } = [];
        public (string?, string?, StudyLevel?, string?, string?, int, int) LastSearch { get; private set; }

        public Task<HomeViewModel> GetHomeAsync() => Task.FromResult(Home);

        public Task<CourseSearchViewModel> SearchCoursesAsync(string? q, string? category, StudyLevel? level, string? sort, string? view, int page, int pageSize)
        {
            LastSearch = (q, category, level, sort, view, page, pageSize);
            return Task.FromResult(Search);
        }

        public Task<CourseDetailViewModel?> GetCourseAsync(string slug) => Task.FromResult(Course);

        public Task<LessonDetailViewModel?> GetLessonAsync(string slug) => Task.FromResult<LessonDetailViewModel?>(null);

        public Task<IReadOnlyList<CategoryCardViewModel>> GetCategoriesAsync() => Task.FromResult(Categories);

        public Task<CategoryDetailViewModel?> GetCategoryAsync(string slug) => Task.FromResult(Category);
    }
}