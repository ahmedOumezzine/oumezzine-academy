using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCourseDeleteCommands(ApplicationDbContext db, IMediaStorage media, IRepository repository) : IAdminCourseDeleteCommands
{
    public async Task<DeleteCourseResult> DeleteAsync(Guid id, CancellationToken token = default)
    {
        if(id==Guid.Empty)return new(false,true,"Cours introuvable.");
        var course=await db.StudyCourses.SingleOrDefaultAsync(x=>x.Id==id,token);
        if(course is null)return new(false,true,"Cours introuvable.");
        if(await db.StudyCourseContents.AnyAsync(x=>x.CourseId==id,token))return new(false,false,"Ce cours ne peut pas être supprimé car il contient encore des chapitres. Supprimez leur contenu puis les chapitres avant de réessayer.");
        if(await db.StudyLearningPathCourses.AnyAsync(x=>x.CourseId==id,token))return new(false,false,"Ce cours est utilisé dans un parcours d’apprentissage. Retirez-le du parcours avant de le supprimer.");
        if(await db.StudyCoursePrerequisites.AnyAsync(x=>x.PrerequisiteCourseId==id,token))return new(false,false,"Ce cours est utilisé comme prérequis par un autre cours. Retirez cette dépendance avant de le supprimer.");
        if(await db.StudyCoursePrerequisites.AnyAsync(x=>x.CourseId==id,token))return new(false,false,"Ce cours possède encore des prérequis. Retirez-les avant de le supprimer.");
        var thumbnail=course.Thumbnail;await repository.HardDeleteAsync(course, token);
        if(!string.IsNullOrWhiteSpace(thumbnail)&&!await db.StudyCourses.AsNoTracking().AnyAsync(x=>x.Thumbnail==thumbnail,token))
        {
            try { media.DeleteIfSafe(thumbnail,"courses",id); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
        }
        return new(true,false,"Le cours a été supprimé.");
    }
}
