using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Application.UseCases;

public sealed class QuizSubmissionHandler(IQuizQueries quizzes) : IQuizSubmissionHandler
{
    public async Task<QuizResultDto?> SubmitAsync(QuizSubmission submission, string languageCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(submission.SelectedAnswers);

        var quiz = await quizzes.GetGradingDataAsync(submission.QuizId, languageCode, cancellationToken);
        if (quiz is null) return null;

        var results = quiz.Questions.Select(question =>
        {
            var selectedAnswerId = submission.SelectedAnswers.GetValueOrDefault(question.QuestionId);
            var selected = question.Answers.FirstOrDefault(answer => answer.AnswerId == selectedAnswerId);
            var correct = question.Answers.FirstOrDefault(answer => answer.IsCorrect);
            return new QuizQuestionResultDto(question.Text, selected?.IsCorrect == true, selected?.Text, correct?.Text ?? "");
        }).ToList();

        return new QuizResultDto(quiz.Title, quiz.Slug, quiz.CourseSlug, results.Count(result => result.IsCorrect), results.Count, results);
    }
}