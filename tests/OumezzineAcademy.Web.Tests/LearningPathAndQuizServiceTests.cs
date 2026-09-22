using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Web.Services;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class LearningPathAndQuizServiceTests
{
    [Fact]
    public async Task Maps_learning_path_list_and_detail()
    {
        var service = new LearningPathCatalogService(new PathQueries(), new FixedLanguage("fr"));
        var list = await service.GetPathsAsync("cat", StudyLevel.Beginner);
        var detail = await service.GetPathAsync("path");

        Assert.Single(list.Paths);
        Assert.Equal("Path", detail!.Title);
        Assert.Single(detail.Courses);
    }

    [Fact]
    public async Task Quiz_service_maps_attempt_and_grades_form_answers()
    {
        var questionId = Guid.NewGuid();
        var answerId = Guid.NewGuid();
        var service = new QuizService(new QuizQueries(questionId, answerId), new SubmissionHandler(), new FixedLanguage("en"));
        var attempt = await service.GetQuizAsync("quiz");
        var result = await service.GradeAsync(Guid.NewGuid(), new FormCollection(new Dictionary<string, StringValues>
        {
            [$"question_{questionId}"] = answerId.ToString(),
            ["invalid"] = "ignored"
        }));

        Assert.Equal("Quiz", attempt!.Title);
        Assert.Equal(1, result!.Score);
        Assert.Single(result.Questions);
    }

    private sealed class FixedLanguage(string code) : ICurrentLanguageService
    { public string LanguageCode => code; }

    private sealed class PathQueries : ILearningPathQueries
    {
        private static LearningPathSummaryDto Summary => new("Path", "path", "Summary", null, StudyLevel.Beginner, "Category", "category", null, 1);

        public Task<IReadOnlyList<LearningPathSummaryDto>> GetPathsAsync(string? category, StudyLevel? level, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LearningPathSummaryDto>>([Summary]);

        public Task<LearningPathDetailsDto?> GetPathAsync(string slug, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult<LearningPathDetailsDto?>(new(Summary, null, null, [new(Guid.NewGuid(), "Course", "course", null, null, 1, 1, 30, 1)]));
    }

    private sealed class QuizQueries(Guid questionId, Guid answerId) : IQuizQueries
    {
        public Task<QuizAttemptDto?> GetQuizAsync(string slug, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult<QuizAttemptDto?>(new(Guid.NewGuid(), "Quiz", "quiz", null, "Course", "course", 1, [new(questionId, "Question", [new(answerId, "Answer")])]));

        public Task<QuizGradingData?> GetGradingDataAsync(Guid quizId, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult<QuizGradingData?>(new("Quiz", "quiz", "course", [new(questionId, "Question", [new(answerId, "Answer", true)])]));
    }

    private sealed class SubmissionHandler : IQuizSubmissionHandler
    {
        public Task<QuizResultDto?> SubmitAsync(QuizSubmission submission, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult<QuizResultDto?>(new("Quiz", "quiz", "course", 1, 1, [new("Question", true, "Answer", "Answer")]));
    }
}