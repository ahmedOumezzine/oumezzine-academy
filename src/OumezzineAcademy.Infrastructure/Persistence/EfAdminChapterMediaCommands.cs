using AhmedOumezzine.EFCore.Repository.Interface;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminChapterMediaCommands(
    IRepository repository,
    IMediaStorage media) : IAdminChapterMediaCommands
{
    public async Task<string?> UploadAsync(
        Guid courseId,
        Guid chapterId,
        MediaUpload upload,
        CancellationToken cancellationToken = default)
    {
        var chapterExists = await repository.ExistsAsync<CourseContent>(
            chapter => chapter.Id == chapterId && chapter.CourseId == courseId,
            cancellationToken);

        if (!chapterExists)
        {
            return null;
        }

        return await media.SaveImageAsync(
            upload,
            "chapters",
            chapterId,
            cancellationToken);
    }
}
