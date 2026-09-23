using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfQuizQueries(IRepository repository) : IQuizQueries
{
    public async Task<QuizAttemptDto?> GetQuizAsync(string slug, string languageCode, CancellationToken cancellationToken = default)
    {
        var quiz = await LoadQuizAsync(cancellationToken);
        var entity = quiz.FirstOrDefault(item => item.Status == StudyStatus.Published
            && item.CourseContent.Course.Status == StudyStatus.Published
            && Published(item.Translations, languageCode)?.Slug == slug);

        if (entity is null)
        {
            return null;
        }

        var translation = Published(entity.Translations, languageCode);
        var questions = entity.QuizQuestions
            .Where(question => Published(question.Translations, languageCode) is not null
                && question.Answers.All(answer => Published(answer.Translations, languageCode) is not null))
            .OrderBy(question => question.Order)
            .Select(question => new QuizQuestionDto(
                question.Id,
                Published(question.Translations, languageCode)!.Text,
                question.Answers
                    .Where(answer => Published(answer.Translations, languageCode) is not null)
                    .OrderBy(answer => answer.CreatedOnUtc)
                    .Select(answer => new QuizAnswerOptionDto(
                        answer.Id,
                        Published(answer.Translations, languageCode)!.Text))
                    .ToList()))
            .ToList();

        if (translation is null || questions.Count != entity.QuizQuestions.Count)
        {
            return null;
        }

        var courseTranslation = Published(entity.CourseContent.Course.Translations, languageCode);
        return new(
            entity.Id,
            translation.Title,
            translation.Slug,
            translation.Summary,
            courseTranslation?.Title ?? string.Empty,
            courseTranslation?.Slug ?? string.Empty,
            entity.QuizQuestions.Count,
            questions);
    }

    public async Task<QuizGradingData?> GetGradingDataAsync(Guid quizId, string languageCode, CancellationToken cancellationToken = default)
    {
        var quizzes = await LoadQuizAsync(cancellationToken);
        var entity = quizzes.FirstOrDefault(item => item.Id == quizId
            && item.Status == StudyStatus.Published
            && Published(item.Translations, languageCode) is not null);

        if (entity is null)
        {
            return null;
        }

        var translation = Published(entity.Translations, languageCode)!;
        var courseSlug = Published(entity.CourseContent.Course.Translations, languageCode)?.Slug ?? string.Empty;
        var questions = entity.QuizQuestions
            .Where(question => Published(question.Translations, languageCode) is not null)
            .OrderBy(question => question.Order)
            .Select(question => new QuizQuestionGradingData(
                question.Id,
                Published(question.Translations, languageCode)!.Text,
                question.Answers
                    .Where(answer => Published(answer.Translations, languageCode) is not null)
                    .Select(answer => new QuizAnswerGradingData(
                        answer.Id,
                        Published(answer.Translations, languageCode)!.Text,
                        answer.IsCorrect))
                    .ToList()))
            .ToList();

        return new(translation.Title, translation.Slug, courseSlug, questions);
    }

    private Task<List<CourseQuiz>> LoadQuizAsync(CancellationToken cancellationToken)
        => repository.GetListAsync<CourseQuiz>(query => query
            .Include(quiz => quiz.Translations)
            .Include(quiz => quiz.CourseContent)
            .ThenInclude(content => content.Course)
            .ThenInclude(course => course.Translations)
            .Include(quiz => quiz.QuizQuestions)
            .ThenInclude(question => question.Translations)
            .Include(quiz => quiz.QuizQuestions)
            .ThenInclude(question => question.Answers)
            .ThenInclude(answer => answer.Translations), cancellationToken);

    private static TTranslation? Published<TTranslation>(IEnumerable<TTranslation> translations, string languageCode)
        where TTranslation : class
        => translations.FirstOrDefault(translation =>
            TranslationLanguage(translation) == languageCode
            && TranslationStatus(translation) == StudyStatus.Published);

    private static string? TranslationLanguage<TTranslation>(TTranslation translation)
        => translation switch
        {
            CourseQuizTranslation value => value.LanguageCode,
            CourseTranslation value => value.LanguageCode,
            QuizQuestionTranslation value => value.LanguageCode,
            QuizAnswerTranslation value => value.LanguageCode,
            _ => null
        };

    private static StudyStatus TranslationStatus<TTranslation>(TTranslation translation)
        => translation switch
        {
            CourseQuizTranslation value => value.PublicationStatus,
            CourseTranslation value => value.PublicationStatus,
            QuizQuestionTranslation value => value.PublicationStatus,
            QuizAnswerTranslation value => value.PublicationStatus,
            _ => StudyStatus.Draft
        };
}
