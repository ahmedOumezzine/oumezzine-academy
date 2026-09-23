using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;

namespace OumezzineAcademy.Web.Services;

public sealed class AdminChapterService(
    IAdminChapterQueries queries,
    IAdminChapterPersistence persistence,
    IStudyLmsCacheInvalidator cache,
    IAdminChapterMediaCommands media)
{
    public async Task<AdminCourseContentViewModel?> ListAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var result = await queries.ListAsync(courseId, cancellationToken);

        if (result is null)
        {
            return null;
        }

        var (course, rows) = result.Value;
        var chapters = rows
            .Select((chapter, index) => new AdminChapterListItem
            {
                Id = chapter.Id,
                Order = chapter.Order,
                FrenchTitle = chapter.FrenchTitle ?? "",
                EnglishTitle = chapter.EnglishTitle ?? "Missing",
                FrenchStatus = chapter.FrenchStatus,
                EnglishStatus = chapter.EnglishStatus,
                LessonsCount = chapter.LessonsCount,
                QuizzesCount = chapter.QuizzesCount,
                IsFirst = index == 0,
                IsLast = index == rows.Count - 1
            })
            .ToList();

        return new()
        {
            CourseId = course.Id,
            DisplayTitle = course.DisplayTitle,
            ThumbnailUrl = course.Thumbnail,
            CategoryTitle = course.CategoryTitle,
            Level = course.Level,
            FrenchStatus = course.FrenchStatus,
            EnglishStatus = course.EnglishStatus,
            ChapterCount = chapters.Count,
            LessonCount = chapters.Sum(chapter => chapter.LessonsCount),
            QuizCount = chapters.Sum(chapter => chapter.QuizzesCount),
            Chapters = chapters
        };
    }

    public async Task<ChapterEditViewModel?> GetFormAsync(
        Guid courseId,
        Guid? id,
        CancellationToken cancellationToken)
    {
        var chapter = await queries.GetForEditAsync(
            courseId,
            id,
            cancellationToken);

        if (chapter is null)
        {
            return null;
        }

        static ChapterTranslationInput ToInput(AdminChapterTranslationDto translation)
            => new()
            {
                Title = translation.Title,
                Summary = translation.Summary,
                PublicationStatus = translation.PublicationStatus
            };

        return new()
        {
            Id = chapter.Id,
            CourseId = chapter.CourseId,
            Order = chapter.Order,
            CourseFrenchTitle = chapter.CourseFrenchTitle,
            CourseEnglishTitle = chapter.CourseEnglishTitle,
            French = ToInput(chapter.French),
            English = ToInput(chapter.English)
        };
    }

    public async Task SaveAsync(
        ChapterEditViewModel model,
        CancellationToken cancellationToken)
    {
        static AdminChapterTranslationDto ToDto(ChapterTranslationInput input)
            => new(input.Title, input.Summary, input.PublicationStatus);

        var result = await persistence.SaveAsync(
            new AdminChapterSaveCommand(
                model.Id == Guid.Empty ? null : model.Id,
                model.CourseId,
                model.Order,
                ToDto(model.French),
                ToDto(model.English)),
            cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error);
        }

        model.Id = result.ChapterId;
        await cache.InvalidatePublicAsync(cancellationToken);
    }

    public async Task MoveAsync(
        Guid courseId,
        Guid id,
        int direction,
        CancellationToken cancellationToken)
    {
        await persistence.MoveAsync(
            courseId,
            id,
            direction,
            cancellationToken);
        await cache.InvalidatePublicAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid courseId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await persistence.DeleteAsync(
            courseId,
            id,
            cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }

        await cache.InvalidatePublicAsync(cancellationToken);
    }

    public async Task<string> UploadImageAsync(
        Guid courseId,
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        var path = await media.UploadAsync(
            courseId,
            id,
            new MediaUpload(
                file.FileName,
                file.ContentType,
                file.Length,
                stream),
            cancellationToken);

        if (path is null)
        {
            throw new KeyNotFoundException();
        }

        return path;
    }
}
