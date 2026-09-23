using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminQuestionCommands(
    ApplicationDbContext db,
    IRepository repository) : IAdminQuestionCommands
{
    public async Task<Guid> SaveAsync(
        AdminQuestionSaveCommand command,
        CancellationToken cancellationToken = default)
    {
        var quiz = await repository.GetByIdAsync<CourseQuiz>(
            command.QuizId,
            cancellationToken);

        if (quiz is null || quiz.CourseContentId != command.ChapterId)
        {
            quiz = null;
        }

        if (quiz is null
            || !await repository.ExistsAsync<CourseContent>(
                entity => entity.Id == command.ChapterId
                    && entity.CourseId == command.CourseId,
                cancellationToken)
            )
        {
            throw new InvalidOperationException("Quiz not found.");
        }

        var answers = command.Answers
            .Where(answer => !string.IsNullOrWhiteSpace(answer.FrenchText)
                || !string.IsNullOrWhiteSpace(answer.EnglishText))
            .ToList();

        if (answers.Count < 2 || answers.Count(answer => answer.IsCorrect) != 1)
        {
            throw new InvalidOperationException(
                "Une question doit avoir au moins deux réponses et exactement une bonne réponse.");
        }

        var question = command.Id.HasValue && command.Id.Value != Guid.Empty
            ? await repository.GetByIdAsync<QuizQuestion>(
                command.Id.Value,
                query => query
                    .Include(entity => entity.Translations)
                    .Include(entity => entity.Answers)
                    .ThenInclude(answer => answer.Translations),
                cancellationToken)
            : null;

        if (question?.CourseQuizId != quiz.Id)
        {
            question = null;
        }

        if (question is null)
        {
            question = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CourseQuizId = quiz.Id,
                CreatedOnUtc = DateTime.UtcNow
            };
        }

        question.Order = Math.Max(1, command.Order);
        question.Text = command.FrenchText
            ?? command.EnglishText
            ?? "Question";

        UpsertQuestionTranslation(
            question,
            "fr",
            command.FrenchText,
            command.FrenchStatus);
        UpsertQuestionTranslation(
            question,
            "en",
            command.EnglishText,
            command.EnglishStatus);

        if (command.Id is null || command.Id == Guid.Empty)
        {
            db.StudyQuizQuestions.Add(question);
        }

        var keptAnswerIds = answers
            .Where(answer => answer.Id != Guid.Empty)
            .Select(answer => answer.Id)
            .ToHashSet();

        db.StudyQuizAnswers.RemoveRange(
            question.Answers.Where(answer => answer.Id != Guid.Empty
                && !keptAnswerIds.Contains(answer.Id)));

        foreach (var answer in answers)
        {
            var answerEntity = answer.Id == Guid.Empty
                ? new QuizAnswer
                {
                    Id = Guid.NewGuid(),
                    QuizQuestionId = question.Id,
                    CreatedOnUtc = DateTime.UtcNow
                }
                : question.Answers.FirstOrDefault(
                    existing => existing.Id == answer.Id)
                    ?? throw new InvalidOperationException(
                        "Une réponse sélectionnée n'appartient pas à cette question.");

            answerEntity.Text = answer.FrenchText
                ?? answer.EnglishText
                ?? "";
            answerEntity.IsCorrect = answer.IsCorrect;

            if (answer.Id == Guid.Empty)
            {
                db.StudyQuizAnswers.Add(answerEntity);
            }

            UpsertAnswerTranslation(answerEntity, "fr", answer.FrenchText);
            UpsertAnswerTranslation(answerEntity, "en", answer.EnglishText);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return question.Id;
    }

    private static void UpsertQuestionTranslation(
        QuizQuestion question,
        string languageCode,
        string? text,
        StudyStatus publicationStatus)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var translation = question.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode)
            ?? new QuizQuestionTranslation
            {
                Id = Guid.NewGuid(),
                QuizQuestionId = question.Id,
                LanguageCode = languageCode
            };

        if (translation.Id != Guid.Empty
            && !question.Translations.Contains(translation))
        {
            question.Translations.Add(translation);
        }

        translation.Text = text.Trim();
        translation.PublicationStatus = publicationStatus;
    }

    private static void UpsertAnswerTranslation(
        QuizAnswer answer,
        string languageCode,
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var translation = answer.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode)
            ?? new QuizAnswerTranslation
            {
                Id = Guid.NewGuid(),
                QuizAnswerId = answer.Id,
                LanguageCode = languageCode
            };

        if (translation.Id != Guid.Empty
            && !answer.Translations.Contains(translation))
        {
            answer.Translations.Add(translation);
        }

        translation.Text = text.Trim();
        translation.PublicationStatus = StudyStatus.Published;
    }

    public async Task MoveAsync(
        AdminQuestionMoveCommand command,
        CancellationToken cancellationToken = default)
    {
        var questions = await repository.GetListAsync<QuizQuestion>(
            query => query
                .Include(question => question.CourseQuiz)
                .ThenInclude(quiz => quiz.CourseContent),
            cancellationToken);
        questions = questions
            .Where(question => question.CourseQuizId == command.QuizId
                && question.CourseQuiz.CourseContentId == command.ChapterId
                && question.CourseQuiz.CourseContent.CourseId == command.CourseId)
            .OrderBy(question => question.Order)
            .ThenBy(question => question.Id)
            .ToList();

        var currentIndex = questions.FindIndex(
            question => question.Id == command.QuestionId);
        var targetIndex = currentIndex + command.Direction;

        if (currentIndex < 0
            || targetIndex < 0
            || targetIndex >= questions.Count)
        {
            return;
        }

        (questions[currentIndex].Order, questions[targetIndex].Order) =
            (questions[targetIndex].Order, questions[currentIndex].Order);

        for (var index = 0; index < questions.Count; index++)
        {
            questions[index].Order = index + 1;
        }

        await repository.UpdateAsync(questions, cancellationToken);
    }

    public async Task<AdminQuestionDeleteStatus> DeleteAsync(
        Guid courseId,
        Guid chapterId,
        Guid quizId,
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        var quiz = await repository.GetByIdAsync<CourseQuiz>(
            quizId,
            cancellationToken);

        var question = quiz is null
            || quiz.CourseContentId != chapterId
            || !await repository.ExistsAsync<CourseContent>(
                entity => entity.Id == chapterId && entity.CourseId == courseId,
                cancellationToken);

        var questionEntity = question
            ? null
            : await repository.GetByIdAsync<QuizQuestion>(
                questionId,
                query => query.Include(entity => entity.Answers),
                cancellationToken);

        if (questionEntity is null || questionEntity.CourseQuizId != quizId)
        {
            return AdminQuestionDeleteStatus.NotFound;
        }

        await repository.HardDeleteAsync(questionEntity, cancellationToken);

        var remainingQuestions = await repository.GetListAsync<QuizQuestion>(cancellationToken);
        remainingQuestions = remainingQuestions
            .Where(entity => entity.CourseQuizId == quizId)
            .OrderBy(entity => entity.Order)
            .ThenBy(entity => entity.Id)
            .ToList();

        for (var index = 0; index < remainingQuestions.Count; index++)
        {
            remainingQuestions[index].Order = index + 1;
        }

        if (remainingQuestions.Count > 0)
        {
            await repository.UpdateAsync(remainingQuestions, cancellationToken);
        }

        return AdminQuestionDeleteStatus.Deleted;
    }
}
