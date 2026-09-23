using AhmedOumezzine.EFCore.Repository.Interface;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCourseMediaCommands(
    IRepository repository,
    IMediaStorage media) : IAdminCourseMediaCommands
{
    public async Task<AdminCourseMediaResult> UploadAsync(
        Guid courseId,
        MediaUpload upload,
        CancellationToken cancellationToken = default)
    {
        var course = await repository.GetByIdAsync<Course>(
            courseId,
            cancellationToken);

        if (course is null)
        {
            return new(false, null);
        }

        var previousThumbnail = course.Thumbnail;
        course.Thumbnail = await media.SaveImageAsync(
            upload,
            "courses",
            courseId,
            cancellationToken);

        await repository.UpdateAsync(course, cancellationToken);
        await RemovePreviousAsync(previousThumbnail, courseId, cancellationToken);

        return new(true, course.Thumbnail);
    }

    public async Task<bool> RemoveAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var course = await repository.GetByIdAsync<Course>(
            courseId,
            cancellationToken);

        if (course is null)
        {
            return false;
        }

        var previousThumbnail = course.Thumbnail;
        course.Thumbnail = null;

        await repository.UpdateAsync(course, cancellationToken);
        await RemovePreviousAsync(previousThumbnail, courseId, cancellationToken);

        return true;
    }

    private async Task RemovePreviousAsync(
        string? previousThumbnail,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(previousThumbnail)
            || await repository.ExistsAsync<Course>(
                entity => entity.Thumbnail == previousThumbnail,
                cancellationToken))
        {
            return;
        }

        try
        {
            media.DeleteIfSafe(previousThumbnail, "courses", courseId);
        }
        catch (Exception exception)
            when (exception is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
        }
    }
}
