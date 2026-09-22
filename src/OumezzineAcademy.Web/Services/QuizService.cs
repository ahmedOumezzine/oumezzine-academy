using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Models.Catalog;

namespace OumezzineAcademy.Web.Services;

public interface IQuizService
{ Task<QuizAttemptViewModel?> GetQuizAsync(string slug); Task<QuizResultViewModel?> GradeAsync(Guid quizId, IFormCollection form); }

/// <summary>Web presentation facade. Persistence and grading are owned by Application/Infrastructure.</summary>
public sealed class QuizService : IQuizService
{
    private readonly IQuizQueries queries; private readonly IQuizSubmissionHandler submissionHandler; private readonly ICurrentLanguageService language;

    public QuizService(IQuizQueries queries, IQuizSubmissionHandler submissionHandler, ICurrentLanguageService language) => (this.queries, this.submissionHandler, this.language) = (queries, submissionHandler, language);

    public async Task<QuizAttemptViewModel?> GetQuizAsync(string slug)
    {
        var quiz = await queries.GetQuizAsync(slug, language.LanguageCode); if (quiz is null) return null;
        return new QuizAttemptViewModel { QuizId = quiz.QuizId, Title = quiz.Title, Slug = quiz.Slug, Summary = quiz.Summary, CourseTitle = quiz.CourseTitle, CourseSlug = quiz.CourseSlug, QuestionCount = quiz.QuestionCount, Questions = quiz.Questions.Select(q => new QuizQuestionViewModel { QuestionId = q.QuestionId, Text = q.Text, Answers = q.Answers.Select(a => new QuizAnswerOptionViewModel { AnswerId = a.AnswerId, Text = a.Text }).ToList() }).ToList() };
    }

    public async Task<QuizResultViewModel?> GradeAsync(Guid quizId, IFormCollection form)
    {
        var selected = new Dictionary<Guid, Guid>(); foreach (var pair in form) { if (pair.Key.StartsWith("question_", StringComparison.Ordinal) && Guid.TryParse(pair.Key[9..], out var q) && Guid.TryParse(pair.Value.FirstOrDefault(), out var a)) selected[q] = a; }
        var result = await submissionHandler.SubmitAsync(new QuizSubmission(quizId, selected), language.LanguageCode); if (result is null) return null;
        return new QuizResultViewModel { QuizTitle = result.QuizTitle, QuizSlug = result.QuizSlug, CourseSlug = result.CourseSlug, Score = result.Score, Total = result.Total, Questions = result.Questions.Select(q => new QuizQuestionResultViewModel { Text = q.Text, IsCorrect = q.IsCorrect, SelectedAnswer = q.SelectedAnswer, CorrectAnswer = q.CorrectAnswer }).ToList() };
    }
}