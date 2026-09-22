using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCategoryPersistence(ApplicationDbContext db, IRepository repository) : IAdminCategoryQueries, IAdminCategoryPersistence
{
    public async Task<IReadOnlyList<AdminCategoryListDto>> ListAsync(CancellationToken token = default) => await db.StudyCourseCategories.AsNoTracking().OrderBy(x => x.Slug).Select(x => new AdminCategoryListDto(x.Id,
        x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? "Non traduit",
        x.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Title).FirstOrDefault() ?? "Non traduit", x.Courses.Count,
        x.Translations.Where(t => t.LanguageCode == "fr").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(),
        x.Translations.Where(t => t.LanguageCode == "en").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault())).ToListAsync(token);

    public async Task<AdminCategoryEditDto?> GetForEditAsync(Guid id, CancellationToken token = default)
    {
        var x = await db.StudyCourseCategories.AsNoTracking().Where(c => c.Id == id).Select(c => new AdminCategoryEditDto(c.Id, c.Courses.Count, c.CreatedOnUtc, c.LastModifiedOnUtc,
            new AdminCategoryTranslationDto(c.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Slug).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Summary).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.MetaTitle).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.MetaDescription).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.PublicationStatus).FirstOrDefault()),
            new AdminCategoryTranslationDto(c.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Title).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Slug).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Summary).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "en").Select(t => t.MetaTitle).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "en").Select(t => t.MetaDescription).FirstOrDefault(), c.Translations.Where(t => t.LanguageCode == "en").Select(t => t.PublicationStatus).FirstOrDefault()))).FirstOrDefaultAsync(token);
        return x;
    }

    public Task<bool> SlugExistsAsync(string languageCode, string slug, Guid excludingId, CancellationToken token = default) => db.StudyCourseCategoryTranslations.AnyAsync(x => x.LanguageCode == languageCode && x.Slug == slug && x.CourseCategoryId != excludingId, token);

    public async Task SaveAsync(AdminCategorySaveCommand command, CancellationToken token = default)
    {
        var entity = !command.Id.HasValue || command.Id.Value == Guid.Empty ? new CourseCategory { Id = Guid.NewGuid(), CreatedOnUtc = DateTime.UtcNow } : await db.StudyCourseCategories.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == command.Id, token) ?? throw new InvalidOperationException("Category not found.");
        if (!command.Id.HasValue || command.Id.Value == Guid.Empty) db.StudyCourseCategories.Add(entity);
        Upsert(entity, "fr", command.French); Upsert(entity, "en", command.English);
        entity.Title = command.French.Title ?? command.English.Title ?? "Category"; entity.Slug = command.French.Slug ?? command.English.Slug ?? "category"; entity.Status = command.French.PublicationStatus;
        db.Entry(entity).Property<bool>("IsDeleted").CurrentValue = false;
        await db.SaveChangesAsync(token);
    }

    public async Task<AdminCategoryDeleteStatus> DeleteAsync(Guid id, CancellationToken token = default)
    {
        var entity = await db.StudyCourseCategories.SingleOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return AdminCategoryDeleteStatus.NotFound;
        if (await db.StudyCourses.AnyAsync(x => x.CourseCategoryId == id, token)) return AdminCategoryDeleteStatus.InUse;
        await repository.HardDeleteAsync(entity, token); return AdminCategoryDeleteStatus.Deleted;
    }

    private void Upsert(CourseCategory entity, string language, AdminCategoryTranslationDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) && string.IsNullOrWhiteSpace(input.Slug)) return;
        var t = entity.Translations.FirstOrDefault(x => x.LanguageCode == language);
        if (t is null) { t = new CourseCategoryTranslation { Id = Guid.NewGuid(), CourseCategoryId = entity.Id, LanguageCode = language }; entity.Translations.Add(t); }
        t.Title = input.Title?.Trim() ?? ""; t.Slug = input.Slug?.Trim() ?? ""; t.Summary = input.Summary; t.MetaTitle = input.MetaTitle; t.MetaDescription = input.MetaDescription; t.PublicationStatus = input.PublicationStatus;
    }
}