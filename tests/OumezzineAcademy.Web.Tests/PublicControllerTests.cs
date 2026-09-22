using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OumezzineAcademy.Controllers;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Web.Services;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class PublicControllerTests
{
    [Fact]
    public async Task LessonDetails_RejectsMissingSlug()
    {
        var result = await new LessonController(new CourseCatalogFake()).Details("");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task LessonDetails_ReturnsNotFoundWhenLessonDoesNotExist()
    {
        var result = await new LessonController(new CourseCatalogFake()).Details("missing");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task LessonDetails_ReturnsViewWhenLessonExists()
    {
        var fake = new CourseCatalogFake { Lesson = new LessonDetailViewModel { Slug = "lesson" } };
        var result = await new LessonController(fake).Details("lesson");
        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(fake.Lesson, view.Model);
    }

    [Fact]
    public async Task LearningPathIndex_ReturnsViewWithServiceModel()
    {
        var fake = new LearningPathCatalogFake();
        var result = await new LearningPathController(fake).Index("backend", StudyLevel.Advanced);
        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(fake.List, view.Model);
    }

    [Fact]
    public async Task LearningPathDetails_RejectsMissingSlug()
    {
        var result = await new LearningPathController(new LearningPathCatalogFake()).Details(" ");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task LearningPathDetails_ReturnsNotFoundWhenPathDoesNotExist()
    {
        var result = await new LearningPathController(new LearningPathCatalogFake()).Details("missing");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task LearningPathDetails_ReturnsViewWhenPathExists()
    {
        var fake = new LearningPathCatalogFake { Detail = new LearningPathDetailViewModel { Slug = "path" } };
        var result = await new LearningPathController(fake).Details("path");
        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(fake.Detail, view.Model);
    }

    [Fact]
    public async Task QuizAttempt_RejectsMissingSlug()
    {
        var result = await new QuizController(new QuizFake()).Attempt(null!);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task QuizAttempt_ReturnsNotFoundWhenQuizDoesNotExist()
    {
        var result = await new QuizController(new QuizFake()).Attempt("missing");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task QuizAttempt_ReturnsViewWhenQuizExists()
    {
        var fake = new QuizFake { AttemptModel = new QuizAttemptViewModel { Slug = "quiz" } };
        var result = await new QuizController(fake).Attempt("quiz");
        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(fake.AttemptModel, view.Model);
    }

    [Fact]
    public async Task QuizSubmit_RejectsEmptyQuizId()
    {
        var result = await new QuizController(new QuizFake()).Submit(Guid.Empty, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task QuizSubmit_ReturnsNotFoundWhenSubmissionHasNoResult()
    {
        var result = await new QuizController(new QuizFake()).Submit(Guid.NewGuid(), new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task QuizSubmit_ReturnsResultViewWhenGradingSucceeds()
    {
        var fake = new QuizFake { ResultModel = new QuizResultViewModel { QuizTitle = "Quiz" } };
        var result = await new QuizController(fake).Submit(Guid.NewGuid(), new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Result", view.ViewName);
        Assert.Same(fake.ResultModel, view.Model);
    }

    private sealed class CourseCatalogFake : ICourseCatalogService
    {
        public LessonDetailViewModel? Lesson { get; init; }
        public Task<HomeViewModel> GetHomeAsync() => Task.FromResult(new HomeViewModel());
        public Task<CourseSearchViewModel> SearchCoursesAsync(string? q, string? category, StudyLevel? level, string? sort, string? view, int page, int pageSize) => Task.FromResult(new CourseSearchViewModel());
        public Task<CourseDetailViewModel?> GetCourseAsync(string slug) => Task.FromResult<CourseDetailViewModel?>(null);
        public Task<LessonDetailViewModel?> GetLessonAsync(string slug) => Task.FromResult(Lesson);
        public Task<IReadOnlyList<CategoryCardViewModel>> GetCategoriesAsync() => Task.FromResult<IReadOnlyList<CategoryCardViewModel>>([]);
        public Task<CategoryDetailViewModel?> GetCategoryAsync(string slug) => Task.FromResult<CategoryDetailViewModel?>(null);
    }

    private sealed class LearningPathCatalogFake : ILearningPathCatalogService
    {
        public LearningPathListViewModel List { get; } = new();
        public LearningPathDetailViewModel? Detail { get; init; }
        public Task<LearningPathListViewModel> GetPathsAsync(string? category, StudyLevel? level) => Task.FromResult(List);
        public Task<LearningPathDetailViewModel?> GetPathAsync(string slug) => Task.FromResult(Detail);
    }

    private sealed class QuizFake : IQuizService
    {
        public QuizAttemptViewModel? AttemptModel { get; init; }
        public QuizResultViewModel? ResultModel { get; init; }
        public Task<QuizAttemptViewModel?> GetQuizAsync(string slug) => Task.FromResult(AttemptModel);
        public Task<QuizResultViewModel?> GradeAsync(Guid quizId, IFormCollection form) => Task.FromResult(ResultModel);
    }
}
