using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Web.Services;

public sealed class AdminLessonService
{
    private readonly IStudyLmsCacheInvalidator _cache;
    private readonly IAdminLessonQueries _queries;
    private readonly IAdminLessonCoreCommands _core;
    private readonly IAdminLessonMediaCommands _media;
    private readonly IAdminLessonDeleteCommands _delete;
    public AdminLessonService(IAdminLessonQueries queries,IAdminLessonCoreCommands core,IAdminLessonMediaCommands media,IAdminLessonDeleteCommands delete,IStudyLmsCacheInvalidator cache) { _queries=queries; _core=core; _media=media; _delete=delete; _cache=cache; }

    public async Task<string> UploadImageAsync(Guid id,IFormFile file,CancellationToken token){await using var stream=file.OpenReadStream();var r=await _media.UploadAsync(id,new MediaUpload(file.FileName,file.ContentType,file.Length,stream),token);if(!r.Found)throw new KeyNotFoundException();return r.StoredPath!;}

    public Task DeleteImageAsync(Guid id,string url,CancellationToken token)=>_media.RemoveAsync(id,url,token);

    public async Task DeleteAsync(Guid courseId,Guid chapterId,Guid id,CancellationToken token){var r=await _delete.DeleteAsync(courseId,chapterId,id,token);if(r.NotFound)throw new KeyNotFoundException();if(!r.Success)throw new InvalidOperationException(r.Message);await _cache.InvalidatePublicAsync(token);}
    public async Task<AdminLessonListViewModel?> ListAsync(Guid courseId,Guid chapterId,CancellationToken token){var r=await _queries.ListAsync(courseId,chapterId,token);if(r is null)return null;var(c,rows)=r.Value;var ls=rows.Select((x,i)=>new AdminLessonListItem{Id=x.Id,Order=x.Order,DurationMinutes=x.DurationMinutes,Slug=x.Slug??"",LastModifiedOnUtc=x.LastModifiedOnUtc,FrenchTitle=x.FrenchTitle??"",EnglishTitle=x.EnglishTitle??"Missing",FrenchStatus=x.FrenchStatus,EnglishStatus=x.EnglishStatus,HasText=x.HasText,HasVideo=x.HasVideo,HasDocument=x.HasDocument,IsFirst=i==0,IsLast=i==rows.Count-1}).ToList();return new(){CourseId=courseId,ChapterId=chapterId,CourseTitle=c.CourseTitle,ChapterTitle=c.ChapterTitle,ChapterOrder=c.ChapterOrder,ChapterFrenchStatus=c.ChapterFrenchStatus,ChapterEnglishStatus=c.ChapterEnglishStatus,Lessons=ls};}
    public async Task<LessonEditViewModel?> GetFormAsync(Guid courseId,Guid chapterId,Guid? id,CancellationToken token){var x=await _queries.GetForEditAsync(courseId,chapterId,id,token);if(x is null)return null;static LessonTranslationInput M(AdminLessonTranslationDto t)=>new(){Title=t.Title,Slug=t.Slug,Summary=t.Summary,ContentHtml=t.ContentHtml,VideoUrl=t.VideoUrl,DocumentUrl=t.DocumentUrl,MetaTitle=t.MetaTitle,MetaDescription=t.MetaDescription,PublicationStatus=t.PublicationStatus};return new(){Id=x.Id,CourseId=x.CourseId,ChapterId=x.ChapterId,Order=x.Order,DurationMinutes=x.DurationMinutes,CourseFrenchTitle=x.CourseFrenchTitle,CourseEnglishTitle=x.CourseEnglishTitle,ChapterTitle=x.ChapterTitle,French=M(x.French),English=M(x.English)};}
    public async Task<Guid> SaveAsync(LessonEditViewModel m,CancellationToken token){static AdminLessonTranslationDto M(LessonTranslationInput x)=>new(x.Title,x.Slug,x.Summary,x.ContentHtml,x.VideoUrl,x.DocumentUrl,x.MetaTitle,x.MetaDescription,x.PublicationStatus);var r=await _core.SaveAsync(new AdminLessonSaveCommand(m.Id==Guid.Empty?null:m.Id,m.CourseId,m.ChapterId,m.Order,m.DurationMinutes,M(m.French),M(m.English)),token);if(!r.Success)throw new InvalidOperationException(r.Error);await _cache.InvalidatePublicAsync(token);return r.LessonId;}
    public Task<bool> SlugExistsAsync(string l,string s,Guid id,CancellationToken t)=>_queries.SlugExistsAsync(l,s,id,t);
    public async Task MoveAsync(Guid courseId,Guid chapterId,Guid id,int direction,CancellationToken t){await _core.MoveAsync(courseId,chapterId,id,direction,t);await _cache.InvalidatePublicAsync(t);}
}

