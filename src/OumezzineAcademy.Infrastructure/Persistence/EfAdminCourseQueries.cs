using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCourseQueries(
    IRepository repository) : IAdminCourseQueries
{
    public async Task<(IReadOnlyList<AdminCourseListDto> Items, int TotalCount)> ListAsync(
        string? search,
        Guid? category,
        StudyLevel? level,
        string? frStatus,
        string? enStatus,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var courses = await repository.GetListAsync<Course>(
            query => query
                .Include(course => course.Translations)
                .Include(course => course.CourseCategory)
                .Include(course => course.CourseContents)
                .ThenInclude(content => content.CourseLessons)
                .Include(course => course.CourseContents)
                .ThenInclude(content => content.CourseQuizzes),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(search))
        {
            courses = courses.Where(course => course.Translations.Any(
                translation => translation.Title.Contains(search)
                    || translation.Slug.Contains(search))).ToList();
        }

        if (category.HasValue)
        {
            courses = courses.Where(course => course.CourseCategoryId == category.Value).ToList();
        }

        if (level.HasValue)
        {
            courses = courses.Where(course => course.Level == level.Value).ToList();
        }

        if (frStatus == "Missing")
        {
            courses = courses.Where(course => !course.Translations.Any(translation => translation.LanguageCode == "fr")).ToList();
        }
        else if (Enum.TryParse<StudyStatus>(frStatus, true, out var frenchStatus))
        {
            courses = courses.Where(course => course.Translations.Any(
                translation => translation.LanguageCode == "fr"
                    && translation.PublicationStatus == frenchStatus)).ToList();
        }

        if (enStatus == "Missing")
        {
            courses = courses.Where(course => !course.Translations.Any(translation => translation.LanguageCode == "en")).ToList();
        }
        else if (Enum.TryParse<StudyStatus>(enStatus, true, out var englishStatus))
        {
            courses = courses.Where(course => course.Translations.Any(
                translation => translation.LanguageCode == "en"
                    && translation.PublicationStatus == englishStatus)).ToList();
        }

        var totalCount = courses.Count;

        var projectedCourses = courses.Select(course => new AdminCourseListDto(
            course.Id,
            course.Status,
            course.Thumbnail,
            course.Slug,
            course.Translations
                .Where(translation => translation.LanguageCode == "fr")
                .Select(translation => translation.Title)
                .FirstOrDefault()
                ?? course.Translations
                    .Where(translation => translation.LanguageCode == "en")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                ?? "Cours sans traduction",
            course.Translations
                .Where(translation => translation.LanguageCode == "fr")
                .Select(translation => translation.Title)
                .FirstOrDefault()
                ?? "Missing",
            course.Translations
                .Where(translation => translation.LanguageCode == "en")
                .Select(translation => translation.Title)
                .FirstOrDefault()
                ?? "Missing",
            course.CourseCategory.Title,
            course.Level,
            course.Translations
                .Where(translation => translation.LanguageCode == "fr")
                .Select(translation => (StudyStatus?)translation.PublicationStatus)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == "en")
                .Select(translation => (StudyStatus?)translation.PublicationStatus)
                .FirstOrDefault(),
            course.CourseContents
                .SelectMany(content => content.CourseLessons)
                .Count(),
            course.CourseContents
                .SelectMany(content => content.CourseQuizzes)
                .Count(),
            course.LastModifiedOnUtc ?? course.CreatedOnUtc));

        var projected = projectedCourses.ToList();

        projected = sort?.ToLowerInvariant() switch
        {
            "title" => projected
                .OrderBy(course => course.DisplayTitle)
                .ToList(),
            "category" => projected
                .OrderBy(course => course.CategoryTitle)
                .ThenBy(course => course.DisplayTitle)
                .ToList(),
            "level" => projected
                .OrderBy(course => course.Level)
                .ThenBy(course => course.DisplayTitle)
                .ToList(),
            _ => projected
                .OrderByDescending(course => course.DateUtc)
                .ToList()
        };

        var pageItems = projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pageItems, totalCount);
    }

    public Task<AdminCourseEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => repository.GetAsync<Course, AdminCourseEditDto>(
            course => course.Id == id,
            course => new AdminCourseEditDto(
                course.Id,
                course.CourseCategoryId,
                course.Level,
                course.Thumbnail,
                course.CreatedOnUtc,
                course.LastModifiedOnUtc,
                Translation(course, "fr"),
                Translation(course, "en"),
                course.Prerequisites
                    .Select(prerequisite => prerequisite.PrerequisiteCourseId)
                    .ToList()),
            cancellationToken);

    private static AdminCourseTranslationDto Translation(
        Course course,
        string languageCode)
        => new(
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Title)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Slug)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Summary)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Overview)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.WhatYouLearn)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Requirements)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.Audience)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.MetaTitle)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.MetaDescription)
                .FirstOrDefault(),
            course.Translations
                .Where(translation => translation.LanguageCode == languageCode)
                .Select(translation => translation.PublicationStatus)
                .FirstOrDefault());

    public async Task<IReadOnlyList<AdminCourseCategoryOptionDto>> GetCategoryOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetListAsync<CourseCategory, AdminCourseCategoryOptionDto>(
            category => new AdminCourseCategoryOptionDto(
                category.Id,
                category.Translations
                    .Where(translation => translation.LanguageCode == "fr")
                    .Select(translation => translation.Title)
                    .FirstOrDefault()
                    ?? category.Translations
                        .Where(translation => translation.LanguageCode == "en")
                        .Select(translation => translation.Title)
                        .FirstOrDefault()
                    ?? "Catégorie sans traduction"),
            cancellationToken);

        return rows
            .OrderBy(row => row.DisplayName)
            .ThenBy(row => row.Id)
            .ToList();
    }

    public async Task<IReadOnlyList<AdminCoursePrerequisiteOptionDto>> GetPrerequisiteOptionsAsync(
        Guid? excludingCourseId = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetListAsync<Course, AdminCoursePrerequisiteOptionDto>(
            course => !excludingCourseId.HasValue
                || course.Id != excludingCourseId.Value,
            course => new AdminCoursePrerequisiteOptionDto(
                course.Id,
                course.Title,
                course.Slug,
                course.Level,
                course.CourseCategory.Title),
            cancellationToken);

        return rows
            .OrderBy(course => course.Title)
            .ToList();
    }

    public async Task<string?> GetThumbnailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => (await repository.GetByIdAsync<Course>(id, cancellationToken))?.Thumbnail;
}
