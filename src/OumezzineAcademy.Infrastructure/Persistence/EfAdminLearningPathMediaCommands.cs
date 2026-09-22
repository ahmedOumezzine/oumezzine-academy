using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace OumezzineAcademy.Infrastructure.Persistence;
public sealed class EfAdminLearningPathMediaCommands(ApplicationDbContext db,IMediaStorage media):IAdminLearningPathMediaCommands
{
 public async Task<string?> UploadAsync(Guid id,MediaUpload upload,CancellationToken t=default){var e=await db.StudyLearningPaths.SingleOrDefaultAsync(x=>x.Id==id,t);if(e is null)return null;var path=await media.SaveImageAsync(upload,"learning-paths",id,t);e.Thumbnail=path;await db.SaveChangesAsync(t);return path;}
}
