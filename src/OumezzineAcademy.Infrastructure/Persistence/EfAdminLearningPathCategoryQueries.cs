using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;
namespace OumezzineAcademy.Infrastructure.Persistence;
public sealed class EfAdminLearningPathCategoryQueries(ApplicationDbContext db) : IAdminLearningPathCategoryQueries
{
 public async Task<IReadOnlyList<AdminLearningPathCategoryListDto>> ListAsync(CancellationToken t=default)=>await db.StudyLearningPathCategories.AsNoTracking().OrderBy(x=>x.Slug).Select(x=>new AdminLearningPathCategoryListDto(x.Id,x.Translations.Where(t=>t.LanguageCode=="fr").Select(t=>t.Title).FirstOrDefault()??x.Title,x.Translations.Where(t=>t.LanguageCode=="en").Select(t=>t.Title).FirstOrDefault()??"Missing",x.LearningPaths.Count,x.Translations.Where(t=>t.LanguageCode=="fr").Select(t=>(StudyStatus?)t.PublicationStatus).FirstOrDefault(),x.Translations.Where(t=>t.LanguageCode=="en").Select(t=>(StudyStatus?)t.PublicationStatus).FirstOrDefault())).ToListAsync(t);
 public async Task<AdminLearningPathCategoryEditDto?> GetForEditAsync(Guid id,CancellationToken t=default)=>await db.StudyLearningPathCategories.AsNoTracking().Where(x=>x.Id==id).Select(x=>new AdminLearningPathCategoryEditDto(x.Id,x.Title,x.Slug,x.Status,x.Translations.Select(y=>new AdminLearningPathCategoryTranslationDto(y.LanguageCode,y.Title,y.Slug,y.Summary,y.MetaTitle,y.MetaDescription,y.PublicationStatus)).ToList(),x.LearningPaths.Count)).FirstOrDefaultAsync(t);
 public Task<bool> SlugExistsAsync(string languageCode,string slug,Guid excludingId,CancellationToken t=default)=>db.StudyLearningPathCategoryTranslations.AsNoTracking().AnyAsync(x=>x.LanguageCode==languageCode&&x.Slug==slug&&x.LearningPathCategoryId!=excludingId,t);
}
