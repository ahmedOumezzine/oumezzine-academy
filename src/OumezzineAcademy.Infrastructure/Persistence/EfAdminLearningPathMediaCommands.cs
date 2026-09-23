using AhmedOumezzine.EFCore.Repository.Interface;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLearningPathMediaCommands(
    IRepository repository,
    IMediaStorage media) : IAdminLearningPathMediaCommands
{
    public async Task<string?> UploadAsync(
        Guid id,
        MediaUpload upload,
        CancellationToken cancellationToken = default)
    {
        var learningPath = await repository.GetByIdAsync<LearningPath>(
            id,
            cancellationToken);

        if (learningPath is null)
        {
            return null;
        }

        var path = await media.SaveImageAsync(
            upload,
            "learning-paths",
            id,
            cancellationToken);

        learningPath.Thumbnail = path;

        await repository.UpdateAsync(learningPath, cancellationToken);

        return path;
    }
}
