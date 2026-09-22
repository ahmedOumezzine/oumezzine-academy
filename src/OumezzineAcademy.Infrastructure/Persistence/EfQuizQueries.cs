using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfQuizQueries(ApplicationDbContext db) : IQuizQueries
{
    public async Task<QuizAttemptDto?> GetQuizAsync(string slug, string languageCode, CancellationToken cancellationToken = default)
    {
        var quiz = await db.Set<CourseQuiz>().AsNoTracking()
            .Where(q => q.Status == StudyStatus.Published && q.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published && t.Slug == slug) && q.CourseContent.Course.Status == StudyStatus.Published)
            .Select(q => new
            {
                q.Id,
                Title = q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                Slug = q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                Summary = q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Summary).FirstOrDefault(),
                CourseTitle = q.CourseContent.Course.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
                CourseSlug = q.CourseContent.Course.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
                QuestionCount = q.QuizQuestions.Count,
                Questions = q.QuizQuestions.Where(question => question.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published) && question.Answers.All(answer => answer.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published))).OrderBy(question => question.Order).Select(question => new QuizQuestionDto(question.Id, question.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Text).FirstOrDefault()!, question.Answers.Where(answer => answer.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).OrderBy(answer => answer.CreatedOnUtc).Select(answer => new QuizAnswerOptionDto(answer.Id, answer.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Text).FirstOrDefault()!)).ToList())).ToList()
            }).FirstOrDefaultAsync(cancellationToken);
        if (quiz is null) return null;
        if (quiz.Questions.Count != quiz.QuestionCount) return null;
        return new QuizAttemptDto(quiz.Id, quiz.Title, quiz.Slug, quiz.Summary, quiz.CourseTitle, quiz.CourseSlug, quiz.QuestionCount, quiz.Questions);
    }

    public async Task<QuizGradingData?> GetGradingDataAsync(Guid quizId, string languageCode, CancellationToken cancellationToken = default)
    {
        var quiz = await db.Set<CourseQuiz>().AsNoTracking().Where(q => q.Id == quizId && q.Status == StudyStatus.Published && q.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).Select(q => new QuizGradingData(
            q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Title).FirstOrDefault()!,
            q.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
            q.CourseContent.Course.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Slug).FirstOrDefault()!,
            q.QuizQuestions.Where(question => question.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).OrderBy(question => question.Order).Select(question => new QuizQuestionGradingData(question.Id, question.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Text).FirstOrDefault()!, question.Answers.Where(answer => answer.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published)).Select(answer => new QuizAnswerGradingData(answer.Id, answer.Translations.Where(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published).Select(t => t.Text).FirstOrDefault()!, answer.IsCorrect)).ToList())).ToList())).FirstOrDefaultAsync(cancellationToken);
        return quiz;
    }
}