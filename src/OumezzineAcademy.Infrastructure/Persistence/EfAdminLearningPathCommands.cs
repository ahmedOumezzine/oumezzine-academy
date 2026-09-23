using AhmedOumezzine.EFCore.Repository.Interface;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLearningPathCommands(
    IRepository repository) : IAdminLearningPathCommands
{
    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var learningPath = await repository.GetByIdAsync<LearningPath>(
            id,
            cancellationToken)
            ?? throw new KeyNotFoundException();

        await repository.HardDeleteAsync(learningPath, cancellationToken);
    }

    public async Task AddCourseAsync(
        Guid pathId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var pathExists = await repository.ExistsAsync<LearningPath>(
            path => path.Id == pathId,
            cancellationToken);
        var courseExists = await repository.ExistsAsync<Course>(
            course => course.Id == courseId,
            cancellationToken);

        if (!pathExists || !courseExists)
        {
            throw new KeyNotFoundException();
        }

        var alreadyAdded = await repository.ExistsAsync<LearningPathCourse>(
            link => link.LearningPathId == pathId
                && link.CourseId == courseId,
            cancellationToken);

        if (alreadyAdded)
        {
            throw new InvalidOperationException("Ce cours est déjà dans le parcours.");
        }

        var order = await repository.CountAsync<LearningPathCourse>(
            link => link.LearningPathId == pathId,
            cancellationToken) + 1;

        await repository.InsertAsync(
            new LearningPathCourse
            {
                Id = Guid.NewGuid(),
                LearningPathId = pathId,
                CourseId = courseId,
                Order = order,
                CreatedOnUtc = DateTime.UtcNow
            },
            cancellationToken);
    }

    public async Task RemoveCourseAsync(
        Guid pathId,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        var link = await repository.GetAsync<LearningPathCourse>(
                item => item.Id == linkId && item.LearningPathId == pathId,
                cancellationToken)
            ?? throw new KeyNotFoundException();

        await repository.HardDeleteAsync(link, cancellationToken);
    }

    public async Task MoveCourseAsync(
        Guid pathId,
        Guid linkId,
        int direction,
        CancellationToken cancellationToken = default)
    {
        var links = (await repository.GetListAsync<LearningPathCourse>(
                link => link.LearningPathId == pathId,
                cancellationToken))
            .OrderBy(link => link.Order)
            .ThenBy(link => link.Id)
            .ToList();

        var currentIndex = links.FindIndex(link => link.Id == linkId);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= links.Count)
        {
            return;
        }

        (links[currentIndex].Order, links[targetIndex].Order) =
            (links[targetIndex].Order, links[currentIndex].Order);

        await repository.UpdateAsync(links, cancellationToken);
    }

    public async Task SynchronizeCoursesAsync(
        Guid pathId,
        IReadOnlyCollection<Guid> selectedCourseIds,
        CancellationToken cancellationToken = default)
    {
        var pathExists = await repository.ExistsAsync<LearningPath>(
            path => path.Id == pathId,
            cancellationToken);

        if (!pathExists)
        {
            throw new KeyNotFoundException();
        }

        var desiredCourseIds = selectedCourseIds
            .Distinct()
            .ToHashSet();

        var existingSelectedCourseCount = await repository.CountAsync<Course>(
                course => desiredCourseIds.Contains(course.Id),
                cancellationToken);

        if (existingSelectedCourseCount != desiredCourseIds.Count)
        {
            throw new InvalidOperationException(
                "Un ou plusieurs cours sélectionnés sont introuvables.");
        }

        var currentLinks = (await repository.GetListAsync<LearningPathCourse>(
                link => link.LearningPathId == pathId,
                cancellationToken))
            .OrderBy(link => link.Order)
            .ThenBy(link => link.Id)
            .ToList();

        var linksToRemove = currentLinks
            .Where(link => !desiredCourseIds.Contains(link.CourseId))
            .ToList();

        if (linksToRemove.Count > 0)
        {
            await repository.HardDeleteAsync(linksToRemove, cancellationToken);
        }

        var keptCourseIds = currentLinks
            .Where(link => desiredCourseIds.Contains(link.CourseId))
            .Select(link => link.CourseId)
            .ToHashSet();

        var nextOrder = currentLinks.Count == 0
            ? 0
            : currentLinks.Max(link => link.Order);

        var linksToAdd = desiredCourseIds
            .Where(courseId => !keptCourseIds.Contains(courseId))
            .OrderBy(courseId => courseId)
            .Select(courseId => new LearningPathCourse
            {
                Id = Guid.NewGuid(),
                LearningPathId = pathId,
                CourseId = courseId,
                Order = ++nextOrder,
                CreatedOnUtc = DateTime.UtcNow
            })
            .ToList();

        if (linksToAdd.Count > 0)
        {
            await repository.InsertRangeAsync(linksToAdd, cancellationToken);
        }

        var remainingLinks = (await repository.GetListAsync<LearningPathCourse>(
                link => link.LearningPathId == pathId,
                cancellationToken))
            .OrderBy(link => link.Order)
            .ThenBy(link => link.Id)
            .ToList();

        for (var index = 0; index < remainingLinks.Count; index++)
        {
            remainingLinks[index].Order = index + 1;
        }

        await repository.UpdateAsync(remainingLinks, cancellationToken);
    }

}
