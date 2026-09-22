using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLessonMediaCommands(ApplicationDbContext db, IMediaStorage media) : IAdminLessonMediaCommands
{
    public async Task<AdminLessonMediaResult> UploadAsync(Guid lessonId, MediaUpload upload, CancellationToken t = default)
    { var e = await db.StudyCourseLessons.SingleOrDefaultAsync(x => x.Id == lessonId, t); if (e is null) return new(false, null); var path = await media.SaveImageAsync(upload, "lessons", lessonId, t); return new(true, path); }

    public async Task RemoveAsync(Guid lessonId, string storedPath, CancellationToken t = default)
    { if (!await db.StudyCourseLessons.AnyAsync(x => x.Id == lessonId, t)) throw new KeyNotFoundException(); if (!media.IsSafeImagePath(storedPath, "lessons", lessonId)) throw new ArgumentException("Chemin d'image invalide.", nameof(storedPath)); media.DeleteIfSafe(storedPath, "lessons", lessonId); }
}

public sealed class EfAdminLessonDeleteCommands(ApplicationDbContext db) : IAdminLessonDeleteCommands
{
    public async Task<AdminLessonDeleteResult> DeleteAsync(Guid courseId, Guid chapterId, Guid lessonId, CancellationToken t = default)
    { var e = await db.StudyCourseLessons.FirstOrDefaultAsync(x => x.Id == lessonId && x.CourseContentId == chapterId && x.CourseContent.CourseId == courseId, t); if (e is null) return new(false, true, "Leçon introuvable."); db.StudyCourseLessons.Remove(e); var a = await db.StudyCourseLessons.Where(x => x.CourseContentId == chapterId && x.Id != lessonId).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); for (var i = 0; i < a.Count; i++) a[i].Order = i + 1; await db.SaveChangesAsync(t); return new(true, false, "La leçon a été supprimée."); }
}