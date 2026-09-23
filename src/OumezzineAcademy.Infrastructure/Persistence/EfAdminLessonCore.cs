using AhmedOumezzine.EFCore.Repository.Interface;
using AhmedOumezzine.EFCore.Repository.Specification;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLessonQueries(
    IRepository repository) : IAdminLessonQueries
{
    public async Task<(AdminLessonContextDto Context, IReadOnlyList<AdminLessonListDto> Lessons)?> ListAsync(Guid courseId, Guid chapterId, CancellationToken cancellationToken = default)
    {
        var content = await repository.GetByIdAsync<CourseContent>(
            chapterId,
            query => query
                .Include(item => item.Course)
                .ThenInclude(course => course.Translations)
                .Include(item => item.Translations),
            cancellationToken);

        var context = content is null || content.CourseId != courseId
            ? null
            : new AdminLessonContextDto(
                courseId,
                chapterId,
                content.Order,
                content.Course.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title ?? content.Course.Title,
                content.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title ?? content.Title,
                content.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.PublicationStatus,
                content.Translations.FirstOrDefault(translation => translation.LanguageCode == "en")?.PublicationStatus);

        if (context is null)
        {
            return null;
        }

        var lessonEntities = await repository.GetListAsync<CourseLesson>(
            query => query.Include(lesson => lesson.Translations),
            cancellationToken);

        var lessons = lessonEntities
            .Where(lesson => lesson.CourseContentId == chapterId)
            .OrderBy(lesson => lesson.Order)
            .ThenBy(lesson => lesson.Id)
            .Select(lesson => new AdminLessonListDto(
                lesson.Id,
                lesson.Order,
                lesson.DurationMinutes,
                lesson.Translations.Where(translation => translation.LanguageCode == "fr").Select(translation => translation.Slug).FirstOrDefault() ?? lesson.Slug,
                lesson.LastModifiedOnUtc,
                lesson.Translations.Where(translation => translation.LanguageCode == "fr").Select(translation => translation.Title).FirstOrDefault() ?? lesson.Title,
                lesson.Translations.Where(translation => translation.LanguageCode == "en").Select(translation => translation.Title).FirstOrDefault() ?? "Missing",
                lesson.Translations.Where(translation => translation.LanguageCode == "fr").Select(translation => (StudyStatus?)translation.PublicationStatus).FirstOrDefault(),
                lesson.Translations.Where(translation => translation.LanguageCode == "en").Select(translation => (StudyStatus?)translation.PublicationStatus).FirstOrDefault(),
                lesson.Translations.Any(translation => translation.ContentHtml != null && translation.ContentHtml != ""),
                lesson.Translations.Any(translation => translation.VideoUrl != null && translation.VideoUrl != ""),
                lesson.Translations.Any(translation => translation.DocumentUrl != null && translation.DocumentUrl != "")))
            .ToList();

        return (context, lessons);
    }

    public async Task<AdminLessonEditDto?> GetForEditAsync(Guid courseId, Guid chapterId, Guid? lessonId, CancellationToken cancellationToken = default)
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

        var lesson = lessonId.HasValue
            ? await repository.GetByIdAsync<CourseLesson>(
                lessonId.Value,
                query => query.Include(item => item.Translations),
                cancellationToken)
            : null;

        if (lesson?.CourseContentId != chapterId)
        {
            lesson = null;
        }

        var order = lesson?.Order
            ?? await repository.CountAsync<CourseLesson>(
                item => item.CourseContentId == chapterId,
                cancellationToken) + 1;

        return new AdminLessonEditDto(
            lesson?.Id ?? Guid.Empty,
            courseId,
            chapterId,
            order,
            lesson?.DurationMinutes,
            chapter.Course.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title ?? chapter.Course.Title,
            chapter.Course.Translations.FirstOrDefault(translation => translation.LanguageCode == "en")?.Title ?? "Missing",
            chapter.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title ?? chapter.Title,
            Translation(lesson, "fr"),
            Translation(lesson, "en"));
    }

    public async Task<bool> SlugExistsAsync(string languageCode, string slug, Guid lessonId, CancellationToken cancellationToken = default)
    {
        var lessons = await repository.GetListAsync<CourseLesson>(
            query => query.Include(lesson => lesson.Translations),
            cancellationToken);

        return lessons.SelectMany(lesson => lesson.Translations).Any(translation =>
            translation.LanguageCode == languageCode
            && translation.Slug == slug
            && translation.CourseLessonId != lessonId);
    }

    private static AdminLessonTranslationDto Translation(CourseLesson? lesson, string languageCode)
    {
        var translation = lesson?.Translations.FirstOrDefault(item => item.LanguageCode == languageCode);
        return new AdminLessonTranslationDto(translation?.Title, translation?.Slug, translation?.Summary, translation?.ContentHtml, translation?.VideoUrl, translation?.DocumentUrl, translation?.MetaTitle, translation?.MetaDescription, translation?.PublicationStatus ?? StudyStatus.Draft);
    }
}

public sealed class EfAdminLessonCoreCommands(
    ApplicationDbContext db,
    IHtmlSanitizer sanitizer,
    IRepository repository) : IAdminLessonCoreCommands
{
    public async Task<(bool Success, Guid LessonId, string? Error)> SaveAsync(AdminLessonSaveCommand command, CancellationToken cancellationToken = default)
    {
        var chapterExists = await repository.ExistsAsync<CourseContent>(
            chapter => chapter.Id == command.ChapterId
                && chapter.CourseId == command.CourseId,
            cancellationToken);

        if (!chapterExists)
        {
            return (false, Guid.Empty, "Chapter not found.");
        }

        var isNew = !command.Id.HasValue || command.Id.Value == Guid.Empty;
        var lesson = isNew
            ? new CourseLesson { Id = Guid.NewGuid(), CourseContentId = command.ChapterId, CreatedOnUtc = DateTime.UtcNow }
            : await repository.GetByIdAsync<CourseLesson>(
                command.Id!.Value,
                query => query.Include(item => item.Translations),
                cancellationToken);

        if (!isNew && lesson?.CourseContentId != command.ChapterId)
        {
            lesson = null;
        }

        if (lesson is null)
        {
            return (false, Guid.Empty, "Lesson not found.");
        }

        lesson.Order = Math.Max(1, command.Order);
        lesson.DurationMinutes = command.DurationMinutes;
        lesson.Title = command.French.Title ?? command.English.Title ?? "Lesson";
        lesson.Slug = command.French.Slug ?? command.English.Slug ?? $"lesson-{lesson.Id:N}";
        lesson.Summary = command.French.Summary;
        lesson.Description = command.French.ContentHtml;
        lesson.VideoUrl = command.French.VideoUrl;
        lesson.DocumentUrl = command.French.DocumentUrl;
        lesson.Status = command.French.PublicationStatus;
        db.Entry(lesson).Property<bool>("IsDeleted").CurrentValue = false;
        UpsertTranslation(lesson, "fr", command.French);
        UpsertTranslation(lesson, "en", command.English);

        if (isNew)
        {
            db.StudyCourseLessons.Add(lesson);
        }
        await NormalizeAsync(command.ChapterId, isNew ? lesson : null, null, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return (true, lesson.Id, null);
    }

    public async Task MoveAsync(Guid courseId, Guid chapterId, Guid lessonId, int direction, CancellationToken cancellationToken = default)
    {
        var specification = new Specification<CourseLesson>
        {
            Conditions =
            {
                lesson => lesson.CourseContentId == chapterId
                    && lesson.CourseContent.CourseId == courseId
            },
            OrderBy = query => query
                .OrderBy(lesson => lesson.Order)
                .ThenBy(lesson => lesson.Id)
        };
        var lessons = await repository.GetListAsync<CourseLesson>(specification, cancellationToken);
        var currentIndex = lessons.FindIndex(lesson => lesson.Id == lessonId);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= lessons.Count)
        {
            return;
        }

        (lessons[currentIndex].Order, lessons[targetIndex].Order) = (lessons[targetIndex].Order, lessons[currentIndex].Order);
        await repository.UpdateAsync(lessons, cancellationToken);
    }

    private void UpsertTranslation(CourseLesson lesson, string languageCode, AdminLessonTranslationDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) && string.IsNullOrWhiteSpace(input.Slug) && string.IsNullOrWhiteSpace(input.ContentHtml))
        {
            return;
        }

        var translation = lesson.Translations.FirstOrDefault(item => item.LanguageCode == languageCode);

        if (translation is null)
        {
            translation = new CourseLessonTranslation { Id = Guid.NewGuid(), CourseLessonId = lesson.Id };
            lesson.Translations.Add(translation);
            db.StudyCourseLessonTranslations.Add(translation);
        }

        translation.LanguageCode = languageCode;
        translation.Title = input.Title?.Trim() ?? "";
        translation.Slug = input.Slug?.Trim() ?? "";
        translation.Summary = input.Summary;
        translation.ContentHtml = sanitizer.Sanitize(input.ContentHtml);
        translation.VideoUrl = input.VideoUrl;
        translation.DocumentUrl = input.DocumentUrl;
        translation.MetaTitle = input.MetaTitle;
        translation.MetaDescription = input.MetaDescription;
        translation.PublicationStatus = input.PublicationStatus;
    }

    private async Task NormalizeAsync(Guid chapterId, CourseLesson? pendingLesson, Guid? excludedLessonId, CancellationToken cancellationToken)
    {
        var lessons = await db.StudyCourseLessons
            .Where(lesson => lesson.CourseContentId == chapterId
                && (!excludedLessonId.HasValue || lesson.Id != excludedLessonId.Value))
            .OrderBy(lesson => lesson.Order)
            .ThenBy(lesson => lesson.Id)
            .ToListAsync(cancellationToken);

        if (pendingLesson is not null && !lessons.Any(lesson => lesson.Id == pendingLesson.Id))
        {
            lessons.Add(pendingLesson);
        }

        lessons = lessons.OrderBy(lesson => lesson.Order).ThenBy(lesson => lesson.Id).ToList();

        for (var index = 0; index < lessons.Count; index++)
        {
            lessons[index].Order = index + 1;
        }
    }
}
