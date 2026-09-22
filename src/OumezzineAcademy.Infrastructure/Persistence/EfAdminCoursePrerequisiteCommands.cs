using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCoursePrerequisiteCommands(ApplicationDbContext db, ICoursePrerequisiteValidator validator) : IAdminCoursePrerequisiteCommands
{
    public async Task<AdminCoursePrerequisiteSyncResult> SynchronizeAsync(Guid courseId, IReadOnlyList<Guid> ids, CancellationToken token = default)
    {
        var selected=ids.Where(x=>x!=Guid.Empty).Distinct().ToHashSet();
        if(selected.Contains(courseId)) return new(false,["A course cannot be its own prerequisite."]);
        if(await db.StudyCourses.CountAsync(x=>selected.Contains(x.Id),token)!=selected.Count) return new(false,["Les prérequis sélectionnés sont invalides."]);
        if(await validator.WouldCreateCycleAsync(courseId,selected,token)) return new(false,["Ce prérequis créerait une dépendance cyclique entre les cours."]);
        var existing=await db.StudyCoursePrerequisites.Where(x=>x.CourseId==courseId).ToListAsync(token);
        db.StudyCoursePrerequisites.RemoveRange(existing.Where(x=>!selected.Contains(x.PrerequisiteCourseId)));
        foreach(var id in selected.Where(x=>existing.All(e=>e.PrerequisiteCourseId!=x))) db.StudyCoursePrerequisites.Add(new CoursePrerequisite{CourseId=courseId,PrerequisiteCourseId=id});
        await db.SaveChangesAsync(token); return new(true,[]);
    }
}
