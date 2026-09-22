using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Application.UseCases;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class QuizSubmissionHandlerTests
{
    [Fact]
    public async Task Grades_selected_answers_and_returns_score()
    {
        var quizId = Guid.NewGuid();
        var correct = Guid.NewGuid();
        var wrong = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var handler = new QuizSubmissionHandler(new QuizStore(new QuizGradingData(
            "Quiz", "quiz", "course", [new(questionId, "Question", [new(correct, "Yes", true), new(wrong, "No", false)])])));

        var result = await handler.SubmitAsync(new(quizId, new Dictionary<Guid, Guid>
        {
            [questionId] = correct
        }), "fr");

        Assert.NotNull(result);
        Assert.Equal(1, result.Score);
        Assert.Equal("Yes", result.Questions[0].SelectedAnswer);
    }

    [Fact]
    public async Task Returns_null_when_quiz_does_not_exist()
    {
        var handler = new QuizSubmissionHandler(new QuizStore(null));
        var result = await handler.SubmitAsync(new(Guid.NewGuid(), new Dictionary<Guid, Guid>()), "en");
        Assert.Null(result);
    }

    private sealed class QuizStore(QuizGradingData? data) : IQuizQueries
    {
        public Task<QuizAttemptDto?> GetQuizAsync(string slug, string languageCode, CancellationToken cancellationToken = default)
            => Task.FromResult<QuizAttemptDto?>(null);

        public Task<QuizGradingData?> GetGradingDataAsync(Guid quizId, string languageCode, CancellationToken cancellationToken = default) => Task.FromResult(data);
    }
}