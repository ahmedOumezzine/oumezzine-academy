using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfCoursePrerequisiteEdges(IRepository repository) : ICoursePrerequisiteEdges
{
    public async Task<IReadOnlyList<CoursePrerequisiteEdge>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var courses = await repository.GetListAsync<Course>(
            query => query.Include(course => course.Prerequisites),
            cancellationToken);

        return courses
            .SelectMany(course => course.Prerequisites.Select(edge =>
                new CoursePrerequisiteEdge(course.Id, edge.PrerequisiteCourseId)))
            .ToList();
    }
}
