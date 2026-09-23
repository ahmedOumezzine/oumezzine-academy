using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfLocalizedSlugQueries(IRepository repository) : ILocalizedSlugQueries
{
    public Task<LocalizedSlugSet?> FindAsync(string entityType, string slug, string languageCode, CancellationToken cancellationToken = default)
        => entityType switch
        {
            "course" => FindCourseAsync(slug, languageCode, cancellationToken),
            "category" => FindCategoryAsync(slug, languageCode, cancellationToken),
            "path" => FindPathAsync(slug, languageCode, cancellationToken),
            "lesson" => FindLessonAsync(slug, languageCode, cancellationToken),
            "quiz" => FindQuizAsync(slug, languageCode, cancellationToken),
            _ => Task.FromResult<LocalizedSlugSet?>(null)
        };

    private async Task<LocalizedSlugSet?> FindCourseAsync(string slug, string languageCode, CancellationToken cancellationToken)
    {
        var entities = await repository.GetListAsync<Course>(query => query.Include(entity => entity.Translations), cancellationToken);
        var entity = entities.FirstOrDefault(item => item.Status == StudyStatus.Published && HasSlug(item.Translations, slug, languageCode));
        return entity is null ? null : Create(entity.Id, entity.Translations);
    }

    private async Task<LocalizedSlugSet?> FindCategoryAsync(string slug, string languageCode, CancellationToken cancellationToken)
    {
        var entities = await repository.GetListAsync<CourseCategory>(query => query.Include(entity => entity.Translations), cancellationToken);
        var entity = entities.FirstOrDefault(item => item.Status == StudyStatus.Published && HasSlug(item.Translations, slug, languageCode));
        return entity is null ? null : Create(entity.Id, entity.Translations);
    }

    private async Task<LocalizedSlugSet?> FindPathAsync(string slug, string languageCode, CancellationToken cancellationToken)
    {
        var entities = await repository.GetListAsync<LearningPath>(query => query.Include(entity => entity.Translations), cancellationToken);
        var entity = entities.FirstOrDefault(item => item.Status == StudyStatus.Published && HasSlug(item.Translations, slug, languageCode));
        return entity is null ? null : Create(entity.Id, entity.Translations);
    }

    private async Task<LocalizedSlugSet?> FindLessonAsync(string slug, string languageCode, CancellationToken cancellationToken)
    {
        var entities = await repository.GetListAsync<CourseLesson>(query => query
            .Include(entity => entity.Translations)
            .Include(entity => entity.CourseContent)
            .ThenInclude(content => content.Course), cancellationToken);
        var entity = entities.FirstOrDefault(item => item.Status == StudyStatus.Published
            && item.CourseContent.Course.Status == StudyStatus.Published
            && HasSlug(item.Translations, slug, languageCode));
        return entity is null ? null : Create(entity.Id, entity.Translations);
    }

    private async Task<LocalizedSlugSet?> FindQuizAsync(string slug, string languageCode, CancellationToken cancellationToken)
    {
        var entities = await repository.GetListAsync<CourseQuiz>(query => query
            .Include(entity => entity.Translations)
            .Include(entity => entity.CourseContent)
            .ThenInclude(content => content.Course), cancellationToken);
        var entity = entities.FirstOrDefault(item => item.Status == StudyStatus.Published
            && item.CourseContent.Course.Status == StudyStatus.Published
            && HasSlug(item.Translations, slug, languageCode));
        return entity is null ? null : Create(entity.Id, entity.Translations);
    }

    private static bool HasSlug<TTranslation>(IEnumerable<TTranslation> translations, string slug, string languageCode)
        => translations.Any(translation =>
            TranslationValues(translation) is { } value
            && value.LanguageCode == languageCode
            && value.Slug == slug);

    private static LocalizedSlugSet Create<TTranslation>(Guid id, IEnumerable<TTranslation> translations)
        => new(id, translations
            .Select(TranslationValues)
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .ToDictionary(value => value.LanguageCode, value => value.Slug));

    private static (string LanguageCode, string Slug)? TranslationValues<TTranslation>(TTranslation translation)
        => translation switch
        {
            CourseTranslation value when value.PublicationStatus == StudyStatus.Published => (value.LanguageCode, value.Slug),
            CourseCategoryTranslation value when value.PublicationStatus == StudyStatus.Published => (value.LanguageCode, value.Slug),
            LearningPathTranslation value when value.PublicationStatus == StudyStatus.Published => (value.LanguageCode, value.Slug),
            CourseLessonTranslation value when value.PublicationStatus == StudyStatus.Published => (value.LanguageCode, value.Slug),
            CourseQuizTranslation value when value.PublicationStatus == StudyStatus.Published => (value.LanguageCode, value.Slug),
            _ => null
        };
}
