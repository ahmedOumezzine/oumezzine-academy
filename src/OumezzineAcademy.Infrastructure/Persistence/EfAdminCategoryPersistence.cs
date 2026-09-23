using AhmedOumezzine.EFCore.Repository.Interface;
using AhmedOumezzine.EFCore.Repository.Specification;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCategoryPersistence(
    IRepository repository) : IAdminCategoryQueries, IAdminCategoryPersistence
{
    public async Task<IReadOnlyList<AdminCategoryListDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var specification = new Specification<CourseCategory>
        {
            OrderBy = query => query.OrderBy(category => category.Slug)
        };

        return await repository.GetListAsync<CourseCategory, AdminCategoryListDto>(
            specification,
            category => new AdminCategoryListDto(
                category.Id,
                category.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? "Non traduit",
                category.Translations
                    .Where(translation => translation.LanguageCode == "en")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? "Non traduit",
                category.Courses.Count,
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

    public async Task<AdminCategoryEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetByIdAsync<CourseCategory>(
            id,
            query => query.Include(entity => entity.Translations),
            cancellationToken);

        if (category is null)
        {
            return null;
        }

        var courseCount = await repository.CountAsync<Course>(
            course => course.CourseCategoryId == id,
            cancellationToken);

        return new(
            category.Id,
            courseCount,
            category.CreatedOnUtc,
            category.LastModifiedOnUtc,
            Translation(category, "fr"),
            Translation(category, "en"));
    }

    public async Task<bool> SlugExistsAsync(
        string languageCode,
        string slug,
        Guid excludingId,
        CancellationToken cancellationToken = default)
    {
        var categories = await repository.GetListAsync<CourseCategory>(
            query => query.Include(category => category.Translations),
            cancellationToken);

        return categories.SelectMany(category => category.Translations).Any(translation =>
            translation.LanguageCode == languageCode
            && translation.Slug == slug
            && translation.CourseCategoryId != excludingId);
    }

    public async Task SaveAsync(
        AdminCategorySaveCommand command,
        CancellationToken cancellationToken = default)
    {
        var isNew = !command.Id.HasValue || command.Id.Value == Guid.Empty;
        var category = isNew
            ? new CourseCategory
            {
                Id = Guid.NewGuid(),
                CreatedOnUtc = DateTime.UtcNow
            }
            : await repository.GetByIdAsync<CourseCategory>(
                command.Id!.Value,
                query => query.Include(entity => entity.Translations),
                cancellationToken)
                ?? throw new InvalidOperationException("Category not found.");

        UpsertTranslation(category, "fr", command.French);
        UpsertTranslation(category, "en", command.English);

        category.Title = command.French.Title
            ?? command.English.Title
            ?? "Category";
        category.Slug = command.French.Slug
            ?? command.English.Slug
            ?? "category";
        category.Status = command.French.PublicationStatus;

        if (isNew)
        {
            await repository.InsertAsync(category, cancellationToken);
        }
        else
        {
            await repository.UpdateAsync(category, cancellationToken);
        }
    }

    public async Task<AdminCategoryDeleteStatus> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetByIdAsync<CourseCategory>(
            id,
            cancellationToken);

        if (category is null)
        {
            return AdminCategoryDeleteStatus.NotFound;
        }

        if (await repository.ExistsAsync<Course>(
                course => course.CourseCategoryId == id,
                cancellationToken))
        {
            return AdminCategoryDeleteStatus.InUse;
        }

        await repository.HardDeleteAsync(category, cancellationToken);

        return AdminCategoryDeleteStatus.Deleted;
    }

    private static AdminCategoryTranslationDto Translation(
        CourseCategory category,
        string languageCode)
        => new(
            category.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Title)
                .FirstOrDefault(),
            category.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Slug)
                .FirstOrDefault(),
            category.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Summary)
                .FirstOrDefault(),
            category.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.MetaTitle)
                .FirstOrDefault(),
            category.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.MetaDescription)
                .FirstOrDefault(),
            category.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.PublicationStatus)
                .FirstOrDefault());

    private static void UpsertTranslation(
        CourseCategory category,
        string languageCode,
        AdminCategoryTranslationDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)
            && string.IsNullOrWhiteSpace(input.Slug))
        {
            return;
        }

        var translation = category.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode);

        if (translation is null)
        {
            translation = new CourseCategoryTranslation
            {
                Id = Guid.NewGuid(),
                CourseCategoryId = category.Id,
                LanguageCode = languageCode
            };

            category.Translations.Add(translation);
        }

        translation.Title = input.Title?.Trim() ?? "";
        translation.Slug = input.Slug?.Trim() ?? "";
        translation.Summary = input.Summary;
        translation.MetaTitle = input.MetaTitle;
        translation.MetaDescription = input.MetaDescription;
        translation.PublicationStatus = input.PublicationStatus;
    }
}
