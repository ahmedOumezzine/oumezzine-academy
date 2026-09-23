using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLessonMediaCommands(
    IRepository repository,
    IMediaStorage media) : IAdminLessonMediaCommands
{
    public async Task<AdminLessonMediaResult> UploadAsync(
        Guid lessonId,
        MediaUpload upload,
        CancellationToken cancellationToken = default)
    {
        var lessonExists = await repository.ExistsAsync<CourseLesson>(
            entity => entity.Id == lessonId,
            cancellationToken);

        if (!lessonExists)
        {
            return new(false, null);
        }

        var path = await media.SaveImageAsync(
            upload,
            "lessons",
            lessonId,
            cancellationToken);

        return new(true, path);
    }

    public async Task RemoveAsync(
        Guid lessonId,
        string storedPath,
        CancellationToken cancellationToken = default)
    {
        var lessonExists = await repository.ExistsAsync<CourseLesson>(
            entity => entity.Id == lessonId,
            cancellationToken);

        if (!lessonExists)
        {
            throw new KeyNotFoundException();
        }

        if (!media.IsSafeImagePath(storedPath, "lessons", lessonId))
        {
            throw new ArgumentException(
                "Chemin d'image invalide.",
                nameof(storedPath));
        }

        media.DeleteIfSafe(storedPath, "lessons", lessonId);
    }
}

public sealed class EfAdminLessonDeleteCommands(
    IRepository repository) : IAdminLessonDeleteCommands
{
    public async Task<AdminLessonDeleteResult> DeleteAsync(
        Guid courseId,
        Guid chapterId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var chapter = await repository.GetByIdAsync<CourseContent>(
            chapterId,
            cancellationToken);

        var lesson = chapter is null || chapter.CourseId != courseId
            ? null
            : await repository.GetAsync<CourseLesson>(
                entity => entity.Id == lessonId
                    && entity.CourseContentId == chapterId,
                cancellationToken);

        if (lesson is null)
        {
            return new(false, true, "Leçon introuvable.");
        }

        var remainingLessons = await repository.GetListAsync<CourseLesson>(cancellationToken);

        var remaining = remainingLessons
            .Where(entity => entity.CourseContentId == chapterId
                && entity.Id != lessonId)
            .OrderBy(entity => entity.Order)
            .ThenBy(entity => entity.Id)
            .ToList();

        for (var index = 0; index < remaining.Count; index++)
        {
            remaining[index].Order = index + 1;
        }

        await repository.HardDeleteAsync(lesson, cancellationToken);

        if (remaining.Count > 0)
        {
            await repository.UpdateAsync(remaining, cancellationToken);
        }

        return new(true, false, "La leçon a été supprimée.");
    }
}
