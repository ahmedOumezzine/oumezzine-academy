using AhmedOumezzine.EFCore.Repository.Interface;
using AhmedOumezzine.EFCore.Repository.Specification;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLearningPathCategoryQueries(
    IRepository repository)
    : IAdminLearningPathCategoryQueries
{
    public async Task<IReadOnlyList<AdminLearningPathCategoryListDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var specification = new Specification<LearningPathCategory>
        {
            OrderBy = query => query.OrderBy(category => category.Slug)
        };

        return await repository.GetListAsync<LearningPathCategory, AdminLearningPathCategoryListDto>(
            specification,
            category => new AdminLearningPathCategoryListDto(
                category.Id,
                category.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? category.Title,
                category.Translations
                    .Where(translation => translation.LanguageCode == "en")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? "Missing",
                category.LearningPaths.Count,
                category.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => (StudyStatus?)translation.PublicationStatus)
                    .FirstOrDefault(),
                category.Translations
                    .Where(translation => translation.LanguageCode == "en")
                    .Select(translation => (StudyStatus?)translation.PublicationStatus)
                    .FirstOrDefault()),
            cancellationToken);
    }

    public async Task<AdminLearningPathCategoryEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await repository.GetAsync<LearningPathCategory, AdminLearningPathCategoryEditDto>(
            category => category.Id == id,
            category => new AdminLearningPathCategoryEditDto(
                category.Id,
                category.Title,
                category.Slug,
                category.Status,
                category.Translations
                    .Select(translation => new AdminLearningPathCategoryTranslationDto(
                        translation.LanguageCode,
                        translation.Title,
                        translation.Slug,
                        translation.Summary,
                        translation.MetaTitle,
                        translation.MetaDescription,
                        translation.PublicationStatus))
                    .ToList(),
                category.LearningPaths.Count),
            cancellationToken);

    public async Task<bool> SlugExistsAsync(
        string languageCode,
        string slug,
        Guid excludingId,
        CancellationToken cancellationToken = default)
    {
        var categories = await repository.GetListAsync<LearningPathCategory>(
            query => query.Include(category => category.Translations),
            cancellationToken);

        return categories.Any(category => category.Id != excludingId
            && category.Translations.Any(translation =>
                translation.LanguageCode == languageCode
                && translation.Slug == slug));
    }
}
