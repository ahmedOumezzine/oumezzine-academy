using OumezzineAcademy.Application.Admin.Catalog;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;

namespace OumezzineAcademy.Web.Services;

public sealed class AdminCategoryService(IAdminCategoryQueries queries, IAdminCategoryPersistence persistence, IStudyLmsCacheInvalidator cache)
{
    public async Task<IReadOnlyList<AdminCategoryListItem>> ListAsync(CancellationToken token) => (await queries.ListAsync(token)).Select(x => new AdminCategoryListItem { Id=x.Id, FrenchTitle=x.FrenchTitle, EnglishTitle=x.EnglishTitle, CourseCount=x.CourseCount, FrenchStatus=x.FrenchStatus, EnglishStatus=x.EnglishStatus }).ToList();
    public async Task<CategoryEditViewModel?> FindAsync(Guid id, CancellationToken token)
    {
        var x=await queries.GetForEditAsync(id,token); if(x is null)return null;
        static CategoryTranslationInput Map(AdminCategoryTranslationDto x)=>new(){Title=x.Title,Slug=x.Slug,Summary=x.Summary,MetaTitle=x.MetaTitle,MetaDescription=x.MetaDescription,PublicationStatus=x.PublicationStatus};
        return new(){Id=x.Id,CourseCount=x.CourseCount,CreatedOnUtc=x.CreatedOnUtc,LastModifiedOnUtc=x.LastModifiedOnUtc,French=Map(x.French),English=Map(x.English)};
    }
    public Task<bool> SlugExistsAsync(string language,string slug,Guid id,CancellationToken token)=>queries.SlugExistsAsync(language,slug,id,token);
    public async Task SaveAsync(CategoryEditViewModel model,CancellationToken token){static AdminCategoryTranslationDto Map(CategoryTranslationInput x)=>new(x.Title,x.Slug,x.Summary,x.MetaTitle,x.MetaDescription,x.PublicationStatus);await persistence.SaveAsync(new AdminCategorySaveCommand(model.Id==Guid.Empty?null:model.Id,Map(model.French),Map(model.English)),token);await cache.InvalidatePublicAsync(token);}
    public async Task<AdminCategoryDeleteStatus> DeleteAsync(Guid id,CancellationToken token){var result=await persistence.DeleteAsync(id,token);if(result==AdminCategoryDeleteStatus.Deleted)await cache.InvalidatePublicAsync(token);return result;}
}

public sealed class AdminCourseService(
    IAdminCourseQueries queries,
    IAdminCourseCoreCommands coreCommands,
    IAdminCoursePrerequisiteCommands prerequisiteCommands,
    IAdminCourseDeleteCommands deleteCommands,
    IAdminCourseMediaCommands mediaCommands,
    IStudyLmsCacheInvalidator cache)
{
    public sealed record SaveResult(bool Success, Guid CourseId, IReadOnlyList<(string Key, string Message)> Errors);
    public async Task<string?> UploadThumbnailAsync(Guid id, IFormFile file, CancellationToken token){await using var stream=file.OpenReadStream();var result=await mediaCommands.UploadAsync(id,new MediaUpload(file.FileName,file.ContentType,file.Length,stream),token);if(result.Found)await cache.InvalidatePublicAsync(token);return result.StoredPath;}
    public async Task<bool> DeleteThumbnailAsync(Guid id,CancellationToken token){var result=await mediaCommands.RemoveAsync(id,token);if(result)await cache.InvalidatePublicAsync(token);return result;}
    public async Task<SaveResult> SaveWithThumbnailAsync(CourseEditViewModel model,CancellationToken token)
    {
        static AdminCourseTranslationDto Map(CourseTranslationInput x)=>new(x.Title,x.Slug,x.Summary,x.Overview,x.WhatYouLearn,x.Requirements,x.Audience,x.MetaTitle,x.MetaDescription,x.PublicationStatus);
        var result=await coreCommands.SaveAsync(new AdminCourseCoreSaveCommand(model.Id==Guid.Empty?null:model.Id,model.CategoryId,model.Level,model.ThumbnailUrl,Map(model.French),Map(model.English)),token);
        if(!result.Success)return new(false,result.CourseId,[(result.ErrorKey??"",result.ErrorMessage??"Impossible d'enregistrer ce cours.")]);
        var sync=await prerequisiteCommands.SynchronizeAsync(result.CourseId,model.PrerequisiteCourseIds.Distinct().ToList(),token);
        if(!sync.Success)return new(false,result.CourseId,sync.Errors.Select(x=>(nameof(model.PrerequisiteCourseIds),x)).ToList());
        if(model.Thumbnail is not null){await using var stream=model.Thumbnail.OpenReadStream();await mediaCommands.UploadAsync(result.CourseId,new MediaUpload(model.Thumbnail.FileName,model.Thumbnail.ContentType,model.Thumbnail.Length,stream),token);}
        await cache.InvalidatePublicAsync(token);return new(true,result.CourseId,[]);
    }
    public async Task<DeleteCourseResult> DeleteAsync(Guid id,CancellationToken token){var result=await deleteCommands.DeleteAsync(id,token);if(result.Success)await cache.InvalidatePublicAsync(token);return result;}
    public Task<string?> GetThumbnailAsync(Guid id,CancellationToken token)=>queries.GetThumbnailAsync(id,token);
    public Task<bool> SlugExistsAsync(string language,string slug,Guid courseId,CancellationToken token)=>coreCommands.SlugExistsAsync(language,slug,courseId,token);
}
