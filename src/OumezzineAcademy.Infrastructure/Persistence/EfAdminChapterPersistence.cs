using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminChapterQueries(
    IRepository repository) : IAdminChapterQueries
{
    public async Task<(AdminChapterCourseDto Course, IReadOnlyList<AdminChapterListDto> Chapters)?> ListAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var courseEntity = await repository.GetByIdAsync<Course>(
            courseId,
            query => query
                .Include(course => course.Translations)
                .Include(course => course.CourseCategory),
            cancellationToken);

        var course = courseEntity is null
            ? null
            : new AdminChapterCourseDto(
                courseEntity.Id,
                courseEntity.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title
                    ?? courseEntity.Translations.FirstOrDefault(translation => translation.LanguageCode == "en")?.Title
                    ?? "Cours sans traduction",
                courseEntity.Thumbnail,
                courseEntity.Level,
                courseEntity.CourseCategory.Title,
                courseEntity.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.PublicationStatus,
                courseEntity.Translations.FirstOrDefault(translation => translation.LanguageCode == "en")?.PublicationStatus);

        if (course is null)
        {
            return null;
        }

        var chapters = await repository.GetListAsync<CourseContent>(
            query => query
                .Include(item => item.Translations)
                .Include(item => item.CourseLessons)
                .Include(item => item.CourseQuizzes),
            cancellationToken);

        var chapterItems = chapters
            .Where(item => item.CourseId == courseId)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Id)
            .Select(item => new AdminChapterListDto(
                item.Id,
                item.Order,
                item.Translations.Where(translation => translation.LanguageCode == "fr").Select(translation => translation.Title).FirstOrDefault() ?? item.Title,
                item.Translations.Where(translation => translation.LanguageCode == "en").Select(translation => translation.Title).FirstOrDefault() ?? "Missing",
                item.Translations.Where(translation => translation.LanguageCode == "fr").Select(translation => (StudyStatus?)translation.PublicationStatus).FirstOrDefault(),
                item.Translations.Where(translation => translation.LanguageCode == "en").Select(translation => (StudyStatus?)translation.PublicationStatus).FirstOrDefault(),
                item.CourseLessons.Count,
                item.CourseQuizzes.Count))
            .ToList();

        return (course, chapterItems);
    }

    public async Task<AdminChapterEditDto?> GetForEditAsync(
        Guid courseId,
        Guid? chapterId,
        CancellationToken cancellationToken = default)
    {
        var course = await repository.GetByIdAsync<Course>(
            courseId,
            query => query.Include(item => item.Translations),
            cancellationToken);

        if (course is null)
        {
            return null;
        }

        var chapter = chapterId.HasValue
            ? await repository.GetByIdAsync<CourseContent>(
                chapterId.Value,
                query => query.Include(item => item.Translations),
                cancellationToken)
            : null;

        if (chapter?.CourseId != courseId)
        {
            chapter = null;
        }

        var order = chapter?.Order
            ?? await repository.CountAsync<CourseContent>(
                item => item.CourseId == courseId,
                cancellationToken) + 1;

        return new AdminChapterEditDto(
            chapter?.Id ?? Guid.Empty,
            courseId,
            order,
            course.Translations.FirstOrDefault(item => item.LanguageCode == "fr")?.Title ?? course.Title,
            course.Translations.FirstOrDefault(item => item.LanguageCode == "en")?.Title ?? "Missing",
            Translation(chapter, "fr"),
            Translation(chapter, "en"));
    }

    private static AdminChapterTranslationDto Translation(
        CourseContent? chapter,
        string languageCode)
    {
        var translation = chapter?.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode);

        return new AdminChapterTranslationDto(
            translation?.Title,
            translation?.Summary,
            translation?.PublicationStatus ?? StudyStatus.Draft);
    }
}

public sealed class EfAdminChapterPersistence(
    IHtmlSanitizer sanitizer,
    IRepository repository) : IAdminChapterPersistence
{
    public async Task<(bool Success, Guid ChapterId, string? Error)> SaveAsync(
        AdminChapterSaveCommand command,
        CancellationToken cancellationToken = default)
    {
        var courseExists = await repository.ExistsAsync<Course>(
            item => item.Id == command.CourseId,
            cancellationToken);

        if (!courseExists)
        {
            return (false, Guid.Empty, "Course not found.");
        }

        var isNew = !command.Id.HasValue || command.Id.Value == Guid.Empty;
        var chapter = isNew
            ? new CourseContent
            {
                Id = Guid.NewGuid(),
                CourseId = command.CourseId,
                CreatedOnUtc = DateTime.UtcNow
            }
            : await repository.GetByIdAsync<CourseContent>(
                command.Id!.Value,
                query => query.Include(item => item.Translations),
                cancellationToken);

        if (!isNew && chapter?.CourseId != command.CourseId)
        {
            chapter = null;
        }

        if (chapter is null)
        {
            return (false, Guid.Empty, "Chapter not found.");
        }

        chapter.Order = Math.Max(1, command.Order);
        chapter.Title = command.French.Title ?? command.English.Title ?? "Chapter";
        chapter.Summary = sanitizer.Sanitize(command.French.Summary);
        chapter.Status = command.French.PublicationStatus;

        UpsertTranslation(chapter, "fr", command.French);
        UpsertTranslation(chapter, "en", command.English);

        await NormalizeAsync(
            command.CourseId,
            isNew ? chapter : null,
            null,
            cancellationToken);

        if (isNew)
        {
            await repository.InsertAsync(chapter, cancellationToken);
        }
        else
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return (true, chapter.Id, null);
    }

    public async Task MoveAsync(
        Guid courseId,
        Guid chapterId,
        int direction,
        CancellationToken cancellationToken = default)
    {
        var chapters = await repository.GetListAsync<CourseContent>(cancellationToken);
        chapters = chapters
            .Where(item => item.CourseId == courseId)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Id)
            .ToList();

        var currentIndex = chapters.FindIndex(item => item.Id == chapterId);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= chapters.Count)
        {
            return;
        }

        (chapters[currentIndex].Order, chapters[targetIndex].Order) =
            (chapters[targetIndex].Order, chapters[currentIndex].Order);

        await NormalizeAsync(
            courseId,
            null,
            null,
            cancellationToken);

        await repository.UpdateAsync(chapters, cancellationToken);
    }

    public async Task<AdminChapterDeleteResult> DeleteAsync(
        Guid courseId,
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        var chapter = await repository.GetByIdAsync<CourseContent>(
            chapterId,
            query => query
                .Include(item => item.CourseLessons)
                .Include(item => item.CourseQuizzes),
            cancellationToken);

        if (chapter?.CourseId != courseId)
        {
            chapter = null;
        }

        if (chapter is null)
        {
            return new(false, true, "Chapitre introuvable.");
        }

        if (chapter.CourseLessons.Count > 0 || chapter.CourseQuizzes.Count > 0)
        {
            return new(false, false, "Ce chapitre contient des leçons ou des quiz et ne peut pas être supprimé.");
        }

        await NormalizeAsync(
            courseId,
            null,
            chapterId,
            cancellationToken);

        await repository.HardDeleteAsync(chapter, cancellationToken);

        return new(true, false, "Le chapitre a été supprimé.");
    }

    private void UpsertTranslation(
        CourseContent chapter,
        string languageCode,
        AdminChapterTranslationDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)
            && string.IsNullOrWhiteSpace(input.Summary))
        {
            return;
        }

        var translation = chapter.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode);

        if (translation is null)
        {
            translation = new CourseContentTranslation
            {
                Id = Guid.NewGuid(),
                CourseContentId = chapter.Id
            };

            chapter.Translations.Add(translation);
        }

        translation.LanguageCode = languageCode;
        translation.Title = input.Title?.Trim() ?? "";
        translation.Summary = sanitizer.Sanitize(input.Summary);
        translation.PublicationStatus = input.PublicationStatus;
    }

    private async Task NormalizeAsync(
        Guid courseId,
        CourseContent? pendingChapter,
        Guid? excludedChapterId,
        CancellationToken cancellationToken)
    {
        var chapters = await repository.GetListAsync<CourseContent>(cancellationToken);
        chapters = chapters
            .Where(item => item.CourseId == courseId
                && (!excludedChapterId.HasValue
                    || item.Id != excludedChapterId.Value))
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Id)
            .ToList();

        if (pendingChapter is not null
            && !chapters.Any(item => item.Id == pendingChapter.Id))
        {
            chapters.Add(pendingChapter);
        }

        chapters = chapters
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Id)
            .ToList();

        for (var index = 0; index < chapters.Count; index++)
        {
            chapters[index].Order = index + 1;
        }
    }
}
