using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfCoursePrerequisiteEdges(ApplicationDbContext db) : ICoursePrerequisiteEdges


{ public async Task<IReadOnlyList<CoursePrerequisiteEdge>> GetAllAsync(CancellationToken token = default) => await db.StudyCoursePrerequisites.AsNoTracking().Select(e => new CoursePrerequisiteEdge(e.CourseId, e.PrerequisiteCourseId)).ToListAsync(token); }