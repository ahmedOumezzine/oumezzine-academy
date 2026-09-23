using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminQuizQueries(
    IRepository repository) : IAdminQuizQueries
{
    public async Task<(AdminQuizContextDto Context, IReadOnlyList<AdminQuizListDto> Quizzes)?> ListAsync(
        Guid courseId,
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        var content = await repository.GetByIdAsync<CourseContent>(
            chapterId,
            query => query
                .Include(item => item.Course)
                .ThenInclude(course => course.Translations)
                .Include(item => item.Translations)
                .Include(item => item.CourseLessons),
            cancellationToken);

        var context = content is null || content.CourseId != courseId
            ? null
            : new AdminQuizContextDto(
                courseId,
                chapterId,
                content.Course.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title ?? content.Course.Title,
                content.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title ?? content.Title,
                content.Order,
                content.CourseLessons.Count,
                content.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.PublicationStatus,
                content.Translations.FirstOrDefault(translation => translation.LanguageCode == "en")?.PublicationStatus);

        if (context is null)
        {
            return null;
        }

        var quizEntities = await repository.GetListAsync<CourseQuiz>(
            query => query
                .Include(quiz => quiz.Translations)
                .Include(quiz => quiz.QuizQuestions),
            cancellationToken);

        var quizzes = quizEntities
            .Where(quiz => quiz.CourseContentId == chapterId)
            .OrderBy(quiz => quiz.Order)
            .ThenBy(quiz => quiz.Id)
            .Select(quiz => new AdminQuizListDto(
                quiz.Id,
                quiz.Order,
                quiz.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? quiz.Translations
                        .Where(translation => translation.LanguageCode == "en")
                        .Select(translation => translation.Title)
                        .FirstOrDefault()
                    ?? "Quiz sans traduction",
                quiz.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => (StudyStatus?)translation.PublicationStatus)
                    .FirstOrDefault(),
                quiz.Translations
                    .Where(translation => translation.LanguageCode == "en")
                    .Select(translation => (StudyStatus?)translation.PublicationStatus)
                    .FirstOrDefault(),
                quiz.QuizQuestions.Count))
            .ToList();

        return (context, quizzes);
    }

    public async Task<AdminQuizEditDto?> GetForEditAsync(
        Guid courseId,
        Guid chapterId,
        Guid? quizId,
        CancellationToken cancellationToken = default)
    {
        var chapter = await repository.GetByIdAsync<CourseContent>(
            chapterId,
            query => query
                .Include(content => content.Course)
                .ThenInclude(course => course.Translations)
                .Include(content => content.Translations),
            cancellationToken);

        if (chapter is null || chapter.CourseId != courseId)
        {
            return null;
        }

        var quiz = quizId.HasValue
            ? await repository.GetByIdAsync<CourseQuiz>(
                quizId.Value,
                query => query.Include(entity => entity.Translations),
                cancellationToken)
            : null;

        if (quiz?.CourseContentId != chapterId)
        {
            quiz = null;
        }

        AdminQuizTranslationDto Translation(string languageCode)
        {
            var translation = quiz?.Translations.FirstOrDefault(
                item => item.LanguageCode == languageCode);

            return new(
                translation?.Title,
                translation?.Slug,
                translation?.Summary,
                translation?.PublicationStatus ?? StudyStatus.Draft);
        }

        var order = quiz?.Order
            ?? await repository.CountAsync<CourseQuiz>(
                entity => entity.CourseContentId == chapterId,
                cancellationToken) + 1;

        return new(
            quiz?.Id ?? Guid.Empty,
            courseId,
            chapterId,
            order,
            chapter.Course.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "fr")?.Title
                ?? chapter.Course.Title,
            chapter.Translations.FirstOrDefault(
                translation => translation.LanguageCode == "fr")?.Title
                ?? chapter.Title,
            Translation("fr"),
            Translation("en"));
    }

    public async Task<bool> SlugExistsAsync(
        string languageCode,
        string slug,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var quizzes = await repository.GetListAsync<CourseQuiz>(
            query => query.Include(quiz => quiz.Translations),
            cancellationToken);

        return quizzes.SelectMany(quiz => quiz.Translations).Any(translation =>
            translation.LanguageCode == languageCode
            && translation.Slug == slug
            && translation.CourseQuizId != id);
    }
}

public sealed class EfAdminQuizPersistence(
    IRepository repository) : IAdminQuizPersistence
{
    public async Task<(bool Success, Guid QuizId, string? Error)> SaveAsync(
        AdminQuizSaveCommand model,
        CancellationToken cancellationToken = default)
    {
        var parentExists = await repository.ExistsAsync<CourseContent>(
            content => content.Id == model.ChapterId && content.CourseId == model.CourseId,
            cancellationToken);

        if (!parentExists)
        {
            return (false, Guid.Empty, "Chapter not found.");
        }

        var isNew = !model.Id.HasValue || model.Id.Value == Guid.Empty;
        var quiz = isNew
            ? new CourseQuiz
            {
                Id = Guid.NewGuid(),
                CourseContentId = model.ChapterId,
                CreatedOnUtc = DateTime.UtcNow
            }
            : await repository.GetByIdAsync<CourseQuiz>(
                model.Id!.Value,
                query => query.Include(entity => entity.Translations),
                cancellationToken);

        if (quiz is null || quiz.CourseContentId != model.ChapterId)
        {
            return (false, Guid.Empty, "Quiz not found.");
        }

        quiz.Order = Math.Max(1, model.Order);
        quiz.Title = model.French.Title ?? model.English.Title ?? "Quiz";
        quiz.Slug = model.French.Slug
            ?? model.English.Slug
            ?? $"quiz-{quiz.Id:N}";
        quiz.Summary = model.French.Summary;
        quiz.Status = model.French.PublicationStatus;

        UpsertTranslation(quiz, "fr", model.French);
        UpsertTranslation(quiz, "en", model.English);

        await NormalizeAsync(
            model.ChapterId,
            isNew ? quiz : null,
            null,
            cancellationToken);

        if (isNew)
        {
            await repository.InsertAsync(quiz, cancellationToken);
        }
        else
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return (true, quiz.Id, null);
    }

    public async Task MoveAsync(
        Guid courseId,
        Guid chapterId,
        Guid id,
        int direction,
        CancellationToken cancellationToken = default)
    {
        var quizzes = await repository.GetListAsync<CourseQuiz>(
            query => query.Include(quiz => quiz.CourseContent),
            cancellationToken);
        quizzes = quizzes
            .Where(quiz => quiz.CourseContentId == chapterId
                && quiz.CourseContent.CourseId == courseId)
            .OrderBy(quiz => quiz.Order)
            .ThenBy(quiz => quiz.Id)
            .ToList();

        var currentIndex = quizzes.FindIndex(quiz => quiz.Id == id);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= quizzes.Count)
        {
            return;
        }

        (quizzes[currentIndex].Order, quizzes[targetIndex].Order) =
            (quizzes[targetIndex].Order, quizzes[currentIndex].Order);

        await NormalizeAsync(
            chapterId,
            null,
            null,
            cancellationToken);

        await repository.UpdateAsync(quizzes, cancellationToken);
    }

    public async Task<AdminQuizDeleteResult> DeleteAsync(
        Guid courseId,
        Guid chapterId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var quiz = await repository.GetByIdAsync<CourseQuiz>(
            id,
            cancellationToken);

        var validParent = quiz is not null
            && quiz.CourseContentId == chapterId
            && await repository.ExistsAsync<CourseContent>(
                entity => entity.Id == chapterId && entity.CourseId == courseId,
                cancellationToken);

        if (!validParent)
        {
            return new(false, true, "Quiz introuvable.");
        }

        if (await repository.ExistsAsync<QuizQuestion>(
                question => question.CourseQuizId == id,
                cancellationToken))
        {
            return new(false, false, "Ce quiz contient des questions. Supprimez-les d'abord.");
        }

        await NormalizeAsync(
            chapterId,
            null,
            id,
            cancellationToken);

        await repository.HardDeleteAsync(quiz!, cancellationToken);

        return new(true, false, "Le quiz a été supprimé.");
    }

    private static void UpsertTranslation(
        CourseQuiz quiz,
        string languageCode,
        AdminQuizTranslationDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)
            && string.IsNullOrWhiteSpace(input.Slug))
        {
            return;
        }

        var translation = quiz.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode);

        if (translation is null)
        {
            translation = new CourseQuizTranslation
            {
                Id = Guid.NewGuid(),
                CourseQuizId = quiz.Id
            };

            quiz.Translations.Add(translation);
        }

        translation.LanguageCode = languageCode;
        translation.Title = input.Title?.Trim() ?? "";
        translation.Slug = input.Slug?.Trim() ?? "";
        translation.Summary = input.Summary;
        translation.PublicationStatus = input.PublicationStatus;
    }

    private async Task NormalizeAsync(
        Guid chapterId,
        CourseQuiz? pendingQuiz,
        Guid? excludedId,
        CancellationToken cancellationToken)
    {
        var quizzes = await repository.GetListAsync<CourseQuiz>(cancellationToken);
        quizzes = quizzes
            .Where(quiz => quiz.CourseContentId == chapterId
                && (!excludedId.HasValue || quiz.Id != excludedId.Value))
            .OrderBy(quiz => quiz.Order)
            .ThenBy(quiz => quiz.Id)
            .ToList();

        if (pendingQuiz is not null
            && !quizzes.Any(quiz => quiz.Id == pendingQuiz.Id))
        {
            quizzes.Add(pendingQuiz);
        }

        quizzes = quizzes
            .OrderBy(quiz => quiz.Order)
            .ThenBy(quiz => quiz.Id)
            .ToList();

        for (var index = 0; index < quizzes.Count; index++)
        {
            quizzes[index].Order = index + 1;
        }
    }
}
