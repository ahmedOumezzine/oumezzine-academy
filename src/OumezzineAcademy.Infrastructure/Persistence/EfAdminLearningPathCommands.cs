using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLearningPathCommands(ApplicationDbContext db, IRepository repository) : IAdminLearningPathCommands
{
    public async Task DeleteAsync(Guid id, CancellationToken t = default)
    { var e = await db.StudyLearningPaths.FirstOrDefaultAsync(x => x.Id == id, t) ?? throw new KeyNotFoundException(); await repository.HardDeleteAsync(e, t); }

    public async Task AddCourseAsync(Guid pathId, Guid courseId, CancellationToken t = default)
    { if (!await db.StudyLearningPaths.AnyAsync(x => x.Id == pathId, t) || !await db.StudyCourses.AnyAsync(x => x.Id == courseId, t)) throw new KeyNotFoundException(); if (await db.StudyLearningPathCourses.AnyAsync(x => x.LearningPathId == pathId && x.CourseId == courseId, t)) throw new InvalidOperationException("Ce cours est déjà dans le parcours."); var order = await db.StudyLearningPathCourses.CountAsync(x => x.LearningPathId == pathId, t) + 1; await repository.InsertAsync(new LearningPathCourse { Id = Guid.NewGuid(), LearningPathId = pathId, CourseId = courseId, Order = order, CreatedOnUtc = DateTime.UtcNow }, t); }

    public async Task RemoveCourseAsync(Guid pathId, Guid linkId, CancellationToken t = default)
    { var e = await db.StudyLearningPathCourses.FirstOrDefaultAsync(x => x.Id == linkId && x.LearningPathId == pathId, t) ?? throw new KeyNotFoundException(); await repository.HardDeleteAsync(e, t); }

    public async Task MoveCourseAsync(Guid pathId, Guid linkId, int direction, CancellationToken t = default)
    { var a = await db.StudyLearningPathCourses.Where(x => x.LearningPathId == pathId).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); var i = a.FindIndex(x => x.Id == linkId); var j = i + direction; if (i < 0 || j < 0 || j >= a.Count) return; (a[i].Order, a[j].Order) = (a[j].Order, a[i].Order); await db.SaveChangesAsync(t); }

    public async Task SynchronizeCoursesAsync(Guid pathId, IReadOnlyCollection<Guid> selectedCourseIds, CancellationToken t = default)
    { if (!await db.StudyLearningPaths.AnyAsync(x => x.Id == pathId, t)) throw new KeyNotFoundException(); var desired = selectedCourseIds.Distinct().ToHashSet(); if (await db.StudyCourses.CountAsync(x => desired.Contains(x.Id), t) != desired.Count) throw new InvalidOperationException("Un ou plusieurs cours sélectionnés sont introuvables."); var current = await db.StudyLearningPathCourses.Where(x => x.LearningPathId == pathId).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); db.StudyLearningPathCourses.RemoveRange(current.Where(x => !desired.Contains(x.CourseId))); var kept = current.Where(x => desired.Contains(x.CourseId)).Select(x => x.CourseId).ToHashSet(); var order = current.Count == 0 ? 0 : current.Max(x => x.Order); foreach (var courseId in desired.Where(x => !kept.Contains(x)).OrderBy(x => x)) { db.StudyLearningPathCourses.Add(new LearningPathCourse { Id = Guid.NewGuid(), LearningPathId = pathId, CourseId = courseId, Order = ++order, CreatedOnUtc = DateTime.UtcNow }); } await db.SaveChangesAsync(t); var remaining = await db.StudyLearningPathCourses.Where(x => x.LearningPathId == pathId).OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(t); for (var i = 0; i < remaining.Count; i++) remaining[i].Order = i + 1; await db.SaveChangesAsync(t); }
}