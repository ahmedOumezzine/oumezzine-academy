using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCoursePrerequisiteCommands(
    ICoursePrerequisiteValidator validator,
    IRepository repository) : IAdminCoursePrerequisiteCommands
{
    public async Task<AdminCoursePrerequisiteSyncResult> SynchronizeAsync(
        Guid courseId,
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var selectedCourseIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        if (selectedCourseIds.Contains(courseId))
        {
            return new(false, ["A course cannot be its own prerequisite."]);
        }

        var selectedCourseCount = await repository.CountAsync<Course>(
                course => selectedCourseIds.Contains(course.Id),
                cancellationToken);

        if (selectedCourseCount != selectedCourseIds.Count)
        {
            return new(false, ["Les prérequis sélectionnés sont invalides."]);
        }

        if (await validator.WouldCreateCycleAsync(
                courseId,
                selectedCourseIds,
                cancellationToken))
        {
            return new(
                false,
                ["Ce prérequis créerait une dépendance cyclique entre les cours."]);
        }

        var course = await repository.GetByIdAsync<Course>(
            courseId,
            query => query.Include(entity => entity.Prerequisites),
            cancellationToken);

        if (course is null)
        {
            return new(false, ["Le cours est introuvable."]);
        }

        course.Prerequisites.RemoveAll(prerequisite =>
            !selectedCourseIds.Contains(prerequisite.PrerequisiteCourseId));

        foreach (var prerequisiteCourseId in selectedCourseIds
                     .Where(prerequisiteCourseId => course.Prerequisites.All(
                         existing => existing.PrerequisiteCourseId != prerequisiteCourseId)))
        {
            course.Prerequisites.Add(
                new CoursePrerequisite
                {
                    CourseId = courseId,
                    PrerequisiteCourseId = prerequisiteCourseId
                });
        }

        await repository.UpdateAsync(course, cancellationToken);

        return new(true, []);
    }
}
