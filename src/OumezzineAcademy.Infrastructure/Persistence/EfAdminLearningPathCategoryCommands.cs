using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLearningPathCategoryCommands(
    IRepository repository) : IAdminLearningPathCategoryCommands
{
    public async Task<AdminLearningPathCategorySaveResult> SaveAsync(
        AdminLearningPathCategorySaveCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = command.Id.HasValue && command.Id.Value != Guid.Empty
            ? await repository.GetByIdAsync<LearningPathCategory>(
                command.Id.Value,
                query => query.Include(entity => entity.Translations),
                cancellationToken)
            : null;

        var created = category is null;

        if (created)
        {
            category = new LearningPathCategory
            {
                Id = Guid.NewGuid(),
                CreatedOnUtc = DateTime.UtcNow,
                Title = command.FrenchTitle
                    ?? command.EnglishTitle
                    ?? "Category",
                Slug = command.FrenchSlug
                    ?? command.EnglishSlug
                    ?? "category",
                Status = command.FrenchStatus
            };
        }
        else
        {
            category!.Status = command.FrenchStatus;
        }

        UpsertTranslation(
            category!,
            "fr",
            command.FrenchTitle,
            command.FrenchSlug,
            command.FrenchSummary,
            command.FrenchMetaTitle,
            command.FrenchMetaDescription,
            command.FrenchStatus);
        UpsertTranslation(
            category!,
            "en",
            command.EnglishTitle,
            command.EnglishSlug,
            command.EnglishSummary,
            command.EnglishMetaTitle,
            command.EnglishMetaDescription,
            command.EnglishStatus);

        if (created)
        {
            await repository.InsertAsync(category!, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return new(category!.Id, created);
    }

    private static void UpsertTranslation(
        LearningPathCategory category,
        string languageCode,
        string? title,
        string? slug,
        string? summary,
        string? metaTitle,
        string? metaDescription,
        StudyStatus publicationStatus)
    {
        if (string.IsNullOrWhiteSpace(title)
            && string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        var translation = category.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode);

        if (translation is null)
        {
            translation = new LearningPathCategoryTranslation
            {
                Id = Guid.NewGuid(),
                LearningPathCategoryId = category.Id,
                LanguageCode = languageCode
            };

            category.Translations.Add(translation);
        }

        translation.Title = title?.Trim() ?? "";
        translation.Slug = slug?.Trim() ?? "";
        translation.Summary = summary;
        translation.MetaTitle = metaTitle;
        translation.MetaDescription = metaDescription;
        translation.PublicationStatus = publicationStatus;
    }

    public async Task<AdminLearningPathCategoryDeleteStatus> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetByIdAsync<LearningPathCategory>(
            id,
            cancellationToken);

        if (category is null)
        {
            return AdminLearningPathCategoryDeleteStatus.NotFound;
        }

        if (await repository.ExistsAsync<LearningPath>(
                path => path.LearningPathCategoryId == id,
                cancellationToken))
        {
            return AdminLearningPathCategoryDeleteStatus.InUse;
        }

        await repository.HardDeleteAsync(category, cancellationToken);

        return AdminLearningPathCategoryDeleteStatus.Deleted;
    }
}
