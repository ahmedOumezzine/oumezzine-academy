using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminChapterMediaCommands(ApplicationDbContext db, IMediaStorage media) : IAdminChapterMediaCommands
{
    public async Task<string?> UploadAsync(Guid courseId, Guid chapterId, MediaUpload upload, CancellationToken cancellationToken = default)
    {
        if (!await db.StudyCourseContents.AnyAsync(x => x.Id == chapterId && x.CourseId == courseId, cancellationToken))
            return null;

        return await media.SaveImageAsync(upload, "chapters", chapterId, cancellationToken);
    }
}
