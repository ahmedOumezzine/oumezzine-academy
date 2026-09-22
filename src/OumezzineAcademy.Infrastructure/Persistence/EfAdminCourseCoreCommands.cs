using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCourseCoreCommands(ApplicationDbContext db) : IAdminCourseCoreCommands
{
    public Task<bool> SlugExistsAsync(string languageCode, string slug, Guid excludingId, CancellationToken token = default) => db.StudyCourseTranslations.AnyAsync(x => x.LanguageCode == languageCode && x.Slug == slug && x.CourseId != excludingId, token);

    public async Task<(bool Success, Guid CourseId, string? ErrorKey, string? ErrorMessage)> SaveAsync(AdminCourseCoreSaveCommand command, CancellationToken token = default)
    {
        if (!await db.StudyCourseCategories.AnyAsync(x => x.Id == command.CategoryId, token)) return (false, command.Id ?? Guid.Empty, "CategoryId", "La catégorie sélectionnée est invalide.");
        if (await SlugExistsAsync("fr", command.French.Slug ?? "", command.Id ?? Guid.Empty, token) || await SlugExistsAsync("en", command.English.Slug ?? "", command.Id ?? Guid.Empty, token)) return (false, command.Id ?? Guid.Empty, "Slug", "Ce slug existe déjà.");
        var entity = !command.Id.HasValue || command.Id.Value == Guid.Empty ? new Course { Id = Guid.NewGuid(), CreatedOnUtc = DateTime.UtcNow } : await db.StudyCourses.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == command.Id.Value, token) ?? null!;
        if (entity is null) return (false, command.Id!.Value, null, "Cours introuvable.");
        entity.CourseCategoryId = command.CategoryId; entity.Level = command.Level; entity.Thumbnail = command.Thumbnail ?? entity.Thumbnail; entity.Status = command.French.PublicationStatus; if (!command.Id.HasValue || command.Id.Value == Guid.Empty) db.StudyCourses.Add(entity);
        Upsert(entity, "fr", command.French); Upsert(entity, "en", command.English); db.Entry(entity).Property<bool>("IsDeleted").CurrentValue = false; await db.SaveChangesAsync(token); return (true, entity.Id, null, null);
    }

    private void Upsert(Course entity, string language, AdminCourseTranslationDto input)
    { if (string.IsNullOrWhiteSpace(input.Title) && string.IsNullOrWhiteSpace(input.Slug)) return; var t = entity.Translations.FirstOrDefault(x => x.LanguageCode == language); if (t is null) { t = new CourseTranslation { Id = Guid.NewGuid(), CourseId = entity.Id, LanguageCode = language }; entity.Translations.Add(t); } t.Title = input.Title?.Trim() ?? ""; t.Slug = input.Slug?.Trim() ?? ""; t.Summary = input.Summary; t.Overview = input.Overview; t.WhatYouLearn = input.WhatYouLearn; t.Requirements = input.Requirements; t.Audience = input.Audience; t.MetaTitle = input.MetaTitle; t.MetaDescription = input.MetaDescription; t.PublicationStatus = input.PublicationStatus; }
}