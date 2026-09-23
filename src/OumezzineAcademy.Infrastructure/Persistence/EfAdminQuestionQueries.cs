using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminQuestionQueries(
    IRepository repository) : IAdminQuestionQueries
{
    public async Task<AdminQuestionQuizContextDto?> ListAsync(
        Guid courseId,
        Guid chapterId,
        Guid quizId,
        CancellationToken cancellationToken = default)
    {
        var quizzes = await repository.GetListAsync<CourseQuiz>(
            query => query
                .Include(quiz => quiz.Translations)
                .Include(quiz => quiz.CourseContent)
                .ThenInclude(content => content.Translations)
                .Include(quiz => quiz.CourseContent)
                .ThenInclude(content => content.Course)
                .ThenInclude(course => course.Translations)
                .Include(quiz => quiz.QuizQuestions)
                .ThenInclude(question => question.Translations)
                .Include(quiz => quiz.QuizQuestions)
                .ThenInclude(question => question.Answers),
            cancellationToken);

        var quiz = quizzes.FirstOrDefault(entity => entity.Id == quizId
            && entity.CourseContentId == chapterId
            && entity.CourseContent.CourseId == courseId);

        if (quiz is null)
        {
            return null;
        }

        var questions = quiz.QuizQuestions
            .OrderBy(question => question.Order)
            .ThenBy(question => question.Id)
            .Select(question => new AdminQuestionListItemDto(
                question.Id,
                question.Order,
                question.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Text)
                    .FirstOrDefault()
                    ?? question.Text,
                question.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => (StudyStatus?)translation.PublicationStatus)
                    .FirstOrDefault(),
                question.Translations
                    .Where(translation => translation.LanguageCode == "en")
                    .Select(translation => (StudyStatus?)translation.PublicationStatus)
                    .FirstOrDefault(),
                question.Answers.Count,
                question.Answers.Any(answer => answer.IsCorrect)))
            .ToList();

        return new(
            courseId,
            chapterId,
            quizId,
            quiz.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title
                ?? quiz.Title,
            quiz.CourseContent.Course.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title
                ?? quiz.CourseContent.Course.Title,
            quiz.CourseContent.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title
                ?? quiz.CourseContent.Title,
            quiz.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.PublicationStatus,
            quiz.Translations.FirstOrDefault(translation => translation.LanguageCode == "en")?.PublicationStatus,
            questions);
    }

    public async Task<AdminQuestionEditDto?> GetFormAsync(
        Guid courseId,
        Guid chapterId,
        Guid quizId,
        Guid? questionId,
        CancellationToken cancellationToken = default)
    {
        var quiz = await repository.GetByIdAsync<CourseQuiz>(
            quizId,
            query => query
                .Include(entity => entity.CourseContent)
                .ThenInclude(content => content.Course)
                .ThenInclude(course => course.Translations)
                .Include(entity => entity.CourseContent)
                .ThenInclude(content => content.Translations)
                .Include(entity => entity.Translations),
            cancellationToken);

        if (quiz is null
            || quiz.CourseContentId != chapterId
            || quiz.CourseContent.CourseId != courseId)
        {
            return null;
        }

        var question = questionId.HasValue
            ? await repository.GetByIdAsync<QuizQuestion>(
                questionId.Value,
                query => query
                    .Include(entity => entity.Translations)
                    .Include(entity => entity.Answers)
                    .ThenInclude(answer => answer.Translations),
                cancellationToken)
            : null;

        if (question?.CourseQuizId != quizId)
        {
            question = null;
        }

        var order = question?.Order
            ?? await repository.CountAsync<QuizQuestion>(
                entity => entity.CourseQuizId == quizId,
                cancellationToken) + 1;

        return new(
            question?.Id ?? Guid.Empty,
            courseId,
            chapterId,
            quizId,
            order,
            quiz.CourseContent.Course.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "fr")?.Title
                ?? quiz.CourseContent.Course.Title,
            quiz.CourseContent.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "fr")?.Title
                ?? quiz.CourseContent.Title,
            quiz.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "fr")?.Title
                ?? quiz.Title,
            question?.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "fr")?.Text,
            question?.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "fr")?.PublicationStatus
                ?? StudyStatus.Draft,
            question?.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "en")?.Text,
            question?.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "en")?.PublicationStatus
                ?? StudyStatus.Draft,
            question?.Answers
                .OrderBy(answer => answer.CreatedOnUtc)
                .Select(answer => new AdminAnswerDto(
                    answer.Id,
                    answer.Translations.FirstOrDefault(
                        translation => translation.LanguageCode == "fr")?.Text,
                    answer.Translations.FirstOrDefault(
                        translation => translation.LanguageCode == "en")?.Text,
                    answer.IsCorrect))
                .ToList()
                ?? new List<AdminAnswerDto>());
    }
}
