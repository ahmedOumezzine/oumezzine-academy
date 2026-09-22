using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCourseMediaCommands(ApplicationDbContext db, IMediaStorage media) : IAdminCourseMediaCommands
{
    public async Task<AdminCourseMediaResult> UploadAsync(Guid courseId, MediaUpload upload, CancellationToken token = default)
    {
        var course = await db.StudyCourses.SingleOrDefaultAsync(x => x.Id == courseId, token);
        if (course is null) return new(false, null);
        var previous = course.Thumbnail;
        course.Thumbnail = await media.SaveImageAsync(upload, "courses", courseId, token);
        await db.SaveChangesAsync(token);
        await RemovePreviousAsync(previous, courseId, token);
        return new(true, course.Thumbnail);
    }

    public async Task<bool> RemoveAsync(Guid courseId, CancellationToken token = default)
    {
        var course = await db.StudyCourses.SingleOrDefaultAsync(x => x.Id == courseId, token);
        if (course is null) return false;
        var previous = course.Thumbnail;
        course.Thumbnail = null;
        await db.SaveChangesAsync(token);
        await RemovePreviousAsync(previous, courseId, token);
        return true;
    }

    private async Task RemovePreviousAsync(string? previous, Guid courseId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(previous) || await db.StudyCourses.AsNoTracking().AnyAsync(x => x.Thumbnail == previous, token)) return;
        try { media.DeleteIfSafe(previous, "courses", courseId); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
    }
}
