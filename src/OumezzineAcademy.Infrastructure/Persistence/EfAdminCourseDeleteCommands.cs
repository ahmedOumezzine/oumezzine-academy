using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCourseDeleteCommands(
    IMediaStorage media,
    IRepository repository) : IAdminCourseDeleteCommands
{
    public async Task<DeleteCourseResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return new(false, true, "Cours introuvable.");
        }

        var course = await repository.GetByIdAsync<Course>(
            id,
            query => query
                .Include(entity => entity.Prerequisites)
                .Include(entity => entity.RequiredByCourses),
            cancellationToken);

        if (course is null)
        {
            return new(false, true, "Cours introuvable.");
        }

        if (await repository.ExistsAsync<CourseContent>(
                content => content.CourseId == id,
                cancellationToken))
        {
            return new(
                false,
                false,
                "Ce cours ne peut pas être supprimé car il contient encore des chapitres. "
                + "Supprimez leur contenu puis les chapitres avant de réessayer.");
        }

        if (await repository.ExistsAsync<LearningPathCourse>(
                link => link.CourseId == id,
                cancellationToken))
        {
            return new(
                false,
                false,
                "Ce cours est utilisé dans un parcours d’apprentissage. "
                + "Retirez-le du parcours avant de le supprimer.");
        }

        if (course.RequiredByCourses.Count > 0)
        {
            return new(
                false,
                false,
                "Ce cours est utilisé comme prérequis par un autre cours. "
                + "Retirez cette dépendance avant de le supprimer.");
        }

        if (course.Prerequisites.Count > 0)
        {
            return new(
                false,
                false,
                "Ce cours possède encore des prérequis. "
                + "Retirez-les avant de le supprimer.");
        }

        var thumbnail = course.Thumbnail;

        await repository.HardDeleteAsync(course, cancellationToken);

        if (!string.IsNullOrWhiteSpace(thumbnail)
            && !await repository.ExistsAsync<Course>(
                entity => entity.Thumbnail == thumbnail,
                cancellationToken))
        {
            try
            {
                media.DeleteIfSafe(thumbnail, "courses", id);
            }
            catch (Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or ArgumentException)
            {
            }
        }

        return new(true, false, "Le cours a été supprimé.");
    }
}
