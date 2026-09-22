using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;
using CacheInvalidator = OumezzineAcademy.Application.Abstractions.IStudyLmsCacheInvalidator;

namespace OumezzineAcademy.Web.Services;

public sealed class AdminLearningPathCategoryService(IAdminLearningPathCategoryQueries queries, IAdminLearningPathCategoryCommands commands, CacheInvalidator? cache = null)
{
    private readonly CacheInvalidator _cache = cache ?? new NullStudyLmsCacheInvalidator();

    public async Task<IReadOnlyList<AdminLearningPathCategoryListItem>> ListAsync(CancellationToken t) => (await queries.ListAsync(t)).Select(x => new AdminLearningPathCategoryListItem { Id = x.Id, FrenchTitle = x.FrenchTitle, EnglishTitle = x.EnglishTitle, PathCount = x.PathCount, FrenchStatus = x.FrenchStatus, EnglishStatus = x.EnglishStatus }).ToList();

    public Task<AdminLearningPathCategoryEditDto?> FindAsync(Guid id, CancellationToken t) => queries.GetForEditAsync(id, t);

    public Task<bool> SlugExistsAsync(string language, string slug, Guid id, CancellationToken t) => queries.SlugExistsAsync(language, slug, id, t);

    public async Task SaveAsync(LearningPathCategoryEditViewModel m, CancellationToken t)
    { var r = await commands.SaveAsync(new AdminLearningPathCategorySaveCommand(m.Id == Guid.Empty ? null : m.Id, m.French.Title, m.French.Slug, m.French.Summary, m.French.MetaTitle, m.French.MetaDescription, m.French.PublicationStatus, m.English.Title, m.English.Slug, m.English.Summary, m.English.MetaTitle, m.English.MetaDescription, m.English.PublicationStatus), t); await _cache.InvalidatePublicAsync(t); }

    public async Task DeleteAsync(Guid id, CancellationToken t)
    { var r = await commands.DeleteAsync(id, t); if (r == AdminLearningPathCategoryDeleteStatus.NotFound) throw new KeyNotFoundException(); if (r == AdminLearningPathCategoryDeleteStatus.InUse) throw new InvalidOperationException("Cette catégorie contient des parcours et ne peut pas être supprimée."); await _cache.InvalidatePublicAsync(t); }
}