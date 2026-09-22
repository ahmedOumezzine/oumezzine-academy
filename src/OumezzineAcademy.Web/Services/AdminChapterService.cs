using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;

namespace OumezzineAcademy.Web.Services;

public sealed class AdminChapterService(IAdminChapterQueries queries, IAdminChapterPersistence persistence, IStudyLmsCacheInvalidator cache, IAdminChapterMediaCommands media)
{
    public async Task<AdminCourseContentViewModel?> ListAsync(Guid courseId, CancellationToken token)
    {
        var result=await queries.ListAsync(courseId,token);if(result is null)return null;var (course,rows)=result.Value;var chapters=rows.Select((x,i)=>new AdminChapterListItem{Id=x.Id,Order=x.Order,FrenchTitle=x.FrenchTitle??"",EnglishTitle=x.EnglishTitle??"Missing",FrenchStatus=x.FrenchStatus,EnglishStatus=x.EnglishStatus,LessonsCount=x.LessonsCount,QuizzesCount=x.QuizzesCount,IsFirst=i==0,IsLast=i==rows.Count-1}).ToList();return new(){CourseId=course.Id,DisplayTitle=course.DisplayTitle,ThumbnailUrl=course.Thumbnail,CategoryTitle=course.CategoryTitle,Level=course.Level,FrenchStatus=course.FrenchStatus,EnglishStatus=course.EnglishStatus,ChapterCount=chapters.Count,LessonCount=chapters.Sum(x=>x.LessonsCount),QuizCount=chapters.Sum(x=>x.QuizzesCount),Chapters=chapters};
    }
    public async Task<ChapterEditViewModel?> GetFormAsync(Guid courseId,Guid? id,CancellationToken token){var x=await queries.GetForEditAsync(courseId,id,token);if(x is null)return null;static ChapterTranslationInput M(AdminChapterTranslationDto t)=>new(){Title=t.Title,Summary=t.Summary,PublicationStatus=t.PublicationStatus};return new(){Id=x.Id,CourseId=x.CourseId,Order=x.Order,CourseFrenchTitle=x.CourseFrenchTitle,CourseEnglishTitle=x.CourseEnglishTitle,French=M(x.French),English=M(x.English)};}
    public async Task SaveAsync(ChapterEditViewModel m,CancellationToken token){static AdminChapterTranslationDto M(ChapterTranslationInput x)=>new(x.Title,x.Summary,x.PublicationStatus);var r=await persistence.SaveAsync(new AdminChapterSaveCommand(m.Id==Guid.Empty?null:m.Id,m.CourseId,m.Order,M(m.French),M(m.English)),token);if(!r.Success)throw new InvalidOperationException(r.Error);m.Id=r.ChapterId;await cache.InvalidatePublicAsync(token);}
    public async Task MoveAsync(Guid courseId,Guid id,int direction,CancellationToken token){await persistence.MoveAsync(courseId,id,direction,token);await cache.InvalidatePublicAsync(token);}
    public async Task DeleteAsync(Guid courseId,Guid id,CancellationToken token){var r=await persistence.DeleteAsync(courseId,id,token);if(!r.Success)throw new InvalidOperationException(r.Message);await cache.InvalidatePublicAsync(token);}
    public async Task<string> UploadImageAsync(Guid courseId,Guid id,IFormFile file,CancellationToken token){await using var stream=file.OpenReadStream();var path=await media.UploadAsync(courseId,id,new MediaUpload(file.FileName,file.ContentType,file.Length,stream),token);if(path is null)throw new KeyNotFoundException();return path;}
}
