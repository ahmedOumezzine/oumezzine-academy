using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;
namespace OumezzineAcademy.Infrastructure.Persistence;
public sealed class EfAdminLearningPathCategoryCommands(ApplicationDbContext db) : IAdminLearningPathCategoryCommands
{
 public async Task<AdminLearningPathCategorySaveResult> SaveAsync(AdminLearningPathCategorySaveCommand c,CancellationToken t=default){var e=c.Id.HasValue&&c.Id.Value!=Guid.Empty?await db.StudyLearningPathCategories.Include(x=>x.Translations).FirstOrDefaultAsync(x=>x.Id==c.Id.Value,t):null;var created=e is null;if(created)e=new LearningPathCategory{Id=Guid.NewGuid(),CreatedOnUtc=DateTime.UtcNow,Title=c.FrenchTitle??c.EnglishTitle??"Category",Slug=c.FrenchSlug??c.EnglishSlug??"category",Status=c.FrenchStatus};else e!.Status=c.FrenchStatus;db.Update(e!);Upsert(e!,"fr",c.FrenchTitle,c.FrenchSlug,c.FrenchSummary,c.FrenchMetaTitle,c.FrenchMetaDescription,c.FrenchStatus);Upsert(e!,"en",c.EnglishTitle,c.EnglishSlug,c.EnglishSummary,c.EnglishMetaTitle,c.EnglishMetaDescription,c.EnglishStatus);await db.SaveChangesAsync(t);return new(e!.Id,created);}
 private void Upsert(LearningPathCategory e,string l,string? title,string? slug,string? summary,string? mt,string? md,StudyStatus st){if(string.IsNullOrWhiteSpace(title)&&string.IsNullOrWhiteSpace(slug))return;var x=e.Translations.FirstOrDefault(x=>x.LanguageCode==l);if(x is null){x=new LearningPathCategoryTranslation{Id=Guid.NewGuid(),LearningPathCategoryId=e.Id,LanguageCode=l};e.Translations.Add(x);}x.Title=title?.Trim()??"";x.Slug=slug?.Trim()??"";x.Summary=summary;x.MetaTitle=mt;x.MetaDescription=md;x.PublicationStatus=st;}
 public async Task<AdminLearningPathCategoryDeleteStatus> DeleteAsync(Guid id,CancellationToken t=default){var e=await db.StudyLearningPathCategories.Include(x=>x.LearningPaths).FirstOrDefaultAsync(x=>x.Id==id,t);if(e is null)return AdminLearningPathCategoryDeleteStatus.NotFound;if(e.LearningPaths.Count>0)return AdminLearningPathCategoryDeleteStatus.InUse;db.Remove(e);await db.SaveChangesAsync(t);return AdminLearningPathCategoryDeleteStatus.Deleted;}
}
