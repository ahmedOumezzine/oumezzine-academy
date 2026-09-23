using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCourseCoreCommands(
    IRepository repository) : IAdminCourseCoreCommands
{
    public async Task<bool> SlugExistsAsync(
        string languageCode,
        string slug,
        Guid excludingId,
        CancellationToken cancellationToken = default)
    {
        var courses = await repository.GetListAsync<Course>(
            query => query.Include(course => course.Translations),
            cancellationToken);

        return courses.SelectMany(course => course.Translations).Any(translation =>
            translation.LanguageCode == languageCode
            && translation.Slug == slug
            && translation.CourseId != excludingId);
    }

    public async Task<(bool Success, Guid CourseId, string? ErrorKey, string? ErrorMessage)> SaveAsync(
        AdminCourseCoreSaveCommand command,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await repository.ExistsAsync<CourseCategory>(
            category => category.Id == command.CategoryId,
            cancellationToken);

        if (!categoryExists)
        {
            return (
                false,
                command.Id ?? Guid.Empty,
                "CategoryId",
                "La catégorie sélectionnée est invalide.");
        }

        var frenchSlugExists = await SlugExistsAsync(
            "fr",
            command.French.Slug ?? "",
            command.Id ?? Guid.Empty,
            cancellationToken);
        var englishSlugExists = await SlugExistsAsync(
            "en",
            command.English.Slug ?? "",
            command.Id ?? Guid.Empty,
            cancellationToken);

        if (frenchSlugExists || englishSlugExists)
        {
            return (
                false,
                command.Id ?? Guid.Empty,
                "Slug",
                "Ce slug existe déjà.");
        }

        var isNew = !command.Id.HasValue || command.Id.Value == Guid.Empty;
        var course = isNew
            ? new Course
            {
                Id = Guid.NewGuid(),
                CreatedOnUtc = DateTime.UtcNow
            }
            : await repository.GetByIdAsync<Course>(
                command.Id!.Value,
                query => query.Include(entity => entity.Translations),
                cancellationToken)
                ?? null!;

        if (course is null)
        {
            return (false, command.Id.GetValueOrDefault(), null, "Cours introuvable.");
        }

        course.CourseCategoryId = command.CategoryId;
        course.Level = command.Level;
        course.Thumbnail = command.Thumbnail ?? course.Thumbnail;
        course.Status = command.French.PublicationStatus;

        UpsertTranslation(course, "fr", command.French);
        UpsertTranslation(course, "en", command.English);

        if (isNew)
        {
            await repository.InsertAsync(course, cancellationToken);
        }
        else
        {
            await repository.UpdateAsync(course, cancellationToken);
        }

        return (true, course.Id, null, null);
    }

    private static void UpsertTranslation(
        Course course,
        string languageCode,
        AdminCourseTranslationDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)
            && string.IsNullOrWhiteSpace(input.Slug))
        {
            return;
        }

        var translation = course.Translations.FirstOrDefault(
            item => item.LanguageCode == languageCode);

        if (translation is null)
        {
            translation = new CourseTranslation
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                LanguageCode = languageCode
            };

            course.Translations.Add(translation);
        }

        translation.Title = input.Title?.Trim() ?? "";
        translation.Slug = input.Slug?.Trim() ?? "";
        translation.Summary = input.Summary;
        translation.Overview = input.Overview;
        translation.WhatYouLearn = input.WhatYouLearn;
        translation.Requirements = input.Requirements;
        translation.Audience = input.Audience;
        translation.MetaTitle = input.MetaTitle;
        translation.MetaDescription = input.MetaDescription;
        translation.PublicationStatus = input.PublicationStatus;
    }
}
