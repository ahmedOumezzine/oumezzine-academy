using AhmedOumezzine.EFCore.Repository.Interface;
using AhmedOumezzine.EFCore.Repository.Specification;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminLearningPathQueries(
    IRepository repository) : IAdminLearningPathQueries
{
    public async Task<IReadOnlyList<AdminLearningPathListDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var specification = new Specification<LearningPath>
        {
            OrderBy = query => query.OrderBy(path => path.Slug)
        };

        return await repository.GetListAsync<LearningPath, AdminLearningPathListDto>(
            specification,
            path => new AdminLearningPathListDto(
                path.Id,
                path.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? path.Translations
                        .Where(translation => translation.LanguageCode == "en")
                        .Select(translation => translation.Title)
                        .FirstOrDefault()
                    ?? "Parcours sans traduction",
                path.LearningPathCategory.Title,
                path.Level,
                path.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => (StudyStatus?)translation.PublicationStatus)
                    .FirstOrDefault(),
                path.Translations
                    .Where(translation => translation.LanguageCode == "en")
                    .Select(translation => (StudyStatus?)translation.PublicationStatus)
                    .FirstOrDefault(),
                path.LearningPathCourses.Count),
            cancellationToken);
    }

    public async Task<AdminLearningPathEditDto?> GetForEditAsync(
        Guid? id,
        CancellationToken cancellationToken = default)
    {
        var categorySpecification = new Specification<LearningPathCategory>
        {
            OrderBy = query => query.OrderBy(category => category.Title)
        };

        var categories = await repository.GetListAsync<
            LearningPathCategory,
            AdminLearningPathCategoryOptionDto>(
            categorySpecification,
            category => new AdminLearningPathCategoryOptionDto(
                category.Id,
                category.Title),
            cancellationToken);

        var learningPath = id.HasValue
            ? await repository.GetByIdAsync<LearningPath>(
                id.Value,
                query => query.Include(path => path.Translations),
                cancellationToken)
            : null;

        AdminLearningPathTranslationDto Translation(string languageCode)
        {
            var translation = learningPath?.Translations.FirstOrDefault(
                item => item.LanguageCode == languageCode);

            return new(
                translation?.Title,
                translation?.Slug,
                translation?.Summary,
                translation?.MetaTitle,
                translation?.MetaDescription,
                translation?.PublicationStatus ?? StudyStatus.Draft);
        }

        var categoryId = learningPath?.LearningPathCategoryId ?? Guid.Empty;

        return new(
            learningPath?.Id ?? Guid.Empty,
            categoryId,
            learningPath?.Level ?? StudyLevel.Beginner,
            learningPath?.Thumbnail,
            categories.FirstOrDefault(category => category.Id == categoryId)?.Title
                ?? "Catégorie",
            Translation("fr"),
            Translation("en"),
            categories);
    }

    public async Task<bool> SlugExistsAsync(
        string languageCode,
        string slug,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var paths = await repository.GetListAsync<LearningPath>(
            query => query.Include(path => path.Translations),
            cancellationToken);

        return paths.SelectMany(path => path.Translations).Any(translation =>
            translation.LanguageCode == languageCode
            && translation.Slug == slug
            && translation.LearningPathId != id);
    }

    public async Task<AdminLearningPathCompositionDto?> GetCompositionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var path = await repository.GetByIdAsync<LearningPath>(
            id,
            query => query.Include(learningPath => learningPath.Translations),
            cancellationToken);

        if (path is null)
        {
            return null;
        }

        var selectedLinks = await repository.GetListAsync<LearningPathCourse>(
            query => query
                .Include(link => link.Course)
                .ThenInclude(course => course.Translations)
                .Include(link => link.Course)
                .ThenInclude(course => course.CourseCategory),
            cancellationToken);

        var selectedCourses = selectedLinks
            .Where(link => link.LearningPathId == id)
            .OrderBy(link => link.Order)
            .Select(link => CourseOption(link, true))
            .ToList();

        var selectedCourseIds = selectedCourses
            .Select(course => course.CourseId)
            .ToHashSet();

        var courses = await repository.GetListAsync<Course>(
            query => query
                .Include(course => course.Translations)
                .Include(course => course.CourseCategory),
            cancellationToken);

        var availableCourses = courses
            .Where(course => !selectedCourseIds.Contains(course.Id))
            .OrderBy(course => course.Title)
            .Select(course => new AdminLearningPathCourseOptionDto(
                Guid.Empty,
                course.Id,
                0,
                course.Title,
                course.Slug,
                course.Level,
                course.CourseCategory.Title,
                course.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.PublicationStatus,
                course.Translations.FirstOrDefault(translation => translation.LanguageCode == "en")?.PublicationStatus,
                false))
            .ToList();

        return new(
            path.Id,
            path.Translations
                .Where(translation => translation.LanguageCode == "fr")
                .Select(translation => translation.Title)
                .FirstOrDefault()
                ?? path.Title,
            selectedCourses,
            availableCourses);
    }

    private static AdminLearningPathCourseOptionDto CourseOption(
        LearningPathCourse link,
        bool selected)
    {
        var course = link.Course;

        return new(
            selected ? link.Id : Guid.Empty,
            course.Id,
            selected ? link.Order : 0,
            course.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.Title
                ?? course.Title,
            course.Slug,
            course.Level,
            course.CourseCategory.Title,
            course.Translations.FirstOrDefault(translation => translation.LanguageCode == "fr")?.PublicationStatus,
            course.Translations.FirstOrDefault(translation => translation.LanguageCode == "en")?.PublicationStatus,
            selected);
    }
}

public sealed class EfAdminLearningPathCoreCommands(
    IHtmlSanitizer sanitizer,
    IRepository repository) : IAdminLearningPathCoreCommands
{
    public async Task<(bool Success, Guid LearningPathId, string? Error)> SaveAsync(
        AdminLearningPathSaveCommand command,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await repository.ExistsAsync<LearningPathCategory>(
                category => category.Id == command.CategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            return (false, Guid.Empty, "Category not found.");
        }

        var isNew = !command.Id.HasValue || command.Id.Value == Guid.Empty;
        var learningPath = isNew
            ? new LearningPath
            {
                Id = Guid.NewGuid(),
                CreatedOnUtc = DateTime.UtcNow
            }
            : await repository.GetByIdAsync<LearningPath>(
                command.Id!.Value,
                query => query.Include(path => path.Translations),
                cancellationToken);

        if (learningPath is null)
        {
            return (false, Guid.Empty, "Learning path not found.");
        }

        learningPath.LearningPathCategoryId = command.CategoryId;
        learningPath.Level = command.Level;
        learningPath.Title = command.French.Title
            ?? command.English.Title
            ?? "Learning path";
        learningPath.Slug = command.French.Slug
            ?? command.English.Slug
            ?? $"path-{learningPath.Id:N}";
        learningPath.Summary = sanitizer.Sanitize(command.French.Summary);
        learningPath.Status = command.French.PublicationStatus;

        if (command.Thumbnail is not null)
        {
            learningPath.Thumbnail = command.Thumbnail;
        }

        UpsertTranslation(learningPath, "fr", command.French);
        UpsertTranslation(learningPath, "en", command.English);

        if (isNew)
        {
            await repository.InsertAsync(learningPath, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return (true, learningPath.Id, null);
    }

    private void UpsertTranslation(
        LearningPath learningPath,
        string languageCode,
        AdminLearningPathTranslationDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)
            && string.IsNullOrWhiteSpace(input.Slug))
        {
            return;
        }

        var translation = learningPath.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode);

        if (translation is null)
        {
            translation = new LearningPathTranslation
            {
                Id = Guid.NewGuid(),
                LearningPathId = learningPath.Id
            };

            learningPath.Translations.Add(translation);
        }

        translation.LanguageCode = languageCode;
        translation.Title = input.Title?.Trim() ?? "";
        translation.Slug = input.Slug?.Trim() ?? "";
        translation.Summary = sanitizer.Sanitize(input.Summary);
        translation.MetaTitle = input.MetaTitle;
        translation.MetaDescription = input.MetaDescription;
        translation.PublicationStatus = input.PublicationStatus;
    }
}
