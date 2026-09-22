using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class EfAdminCourseQueries(ApplicationDbContext db) : IAdminCourseQueries
{
    public async Task<(IReadOnlyList<AdminCourseListDto> Items, int TotalCount)> ListAsync(string? search, Guid? category, StudyLevel? level, string? frStatus, string? enStatus, string? sort, int page, int pageSize, CancellationToken token = default)
    {
        var q = db.StudyCourses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Translations.Any(t => t.Title.Contains(search) || t.Slug.Contains(search)));
        if (category.HasValue) q = q.Where(x => x.CourseCategoryId == category.Value);
        if (level.HasValue) q = q.Where(x => x.Level == level.Value);
        if (frStatus == "Missing") q = q.Where(x => !x.Translations.Any(t => t.LanguageCode == "fr")); else if (Enum.TryParse<StudyStatus>(frStatus, true, out var fs)) q = q.Where(x => x.Translations.Any(t => t.LanguageCode == "fr" && t.PublicationStatus == fs));
        if (enStatus == "Missing") q = q.Where(x => !x.Translations.Any(t => t.LanguageCode == "en")); else if (Enum.TryParse<StudyStatus>(enStatus, true, out var es)) q = q.Where(x => x.Translations.Any(t => t.LanguageCode == "en" && t.PublicationStatus == es));
        var total = await q.CountAsync(token);
        var p = q.Select(x => new AdminCourseListDto(x.Id, x.Status, x.Thumbnail, x.Slug, x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? x.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Title).FirstOrDefault() ?? "Cours sans traduction", x.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? "Missing", x.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Title).FirstOrDefault() ?? "Missing", x.CourseCategory.Title, x.Level, x.Translations.Where(t => t.LanguageCode == "fr").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == "en").Select(t => (StudyStatus?)t.PublicationStatus).FirstOrDefault(), x.CourseContents.SelectMany(c => c.CourseLessons).Count(), x.CourseContents.SelectMany(c => c.CourseQuizzes).Count(), x.LastModifiedOnUtc ?? x.CreatedOnUtc));
        var all = await p.ToListAsync(token); all = sort?.ToLowerInvariant() switch { "title" => all.OrderBy(x => x.DisplayTitle).ToList(), "category" => all.OrderBy(x => x.CategoryTitle).ThenBy(x => x.DisplayTitle).ToList(), "level" => all.OrderBy(x => x.Level).ThenBy(x => x.DisplayTitle).ToList(), _ => all.OrderByDescending(x => x.DateUtc).ToList() };
        return (all.Skip((page - 1) * pageSize).Take(pageSize).ToList(), total);
    }

    public Task<AdminCourseEditDto?> GetForEditAsync(Guid id, CancellationToken token = default) => db.StudyCourses.AsNoTracking().Where(x => x.Id == id).Select(x => new AdminCourseEditDto(x.Id, x.CourseCategoryId, x.Level, x.Thumbnail, x.CreatedOnUtc, x.LastModifiedOnUtc, Translation(x, "fr"), Translation(x, "en"), x.Prerequisites.Select(p => p.PrerequisiteCourseId).ToList())).FirstOrDefaultAsync(token);

    private static AdminCourseTranslationDto Translation(Course x, string language) => new(x.Translations.Where(t => t.LanguageCode == language).Select(t => t.Title).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.Slug).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.Summary).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.Overview).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.WhatYouLearn).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.Requirements).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.Audience).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.MetaTitle).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.MetaDescription).FirstOrDefault(), x.Translations.Where(t => t.LanguageCode == language).Select(t => t.PublicationStatus).FirstOrDefault());

    public async Task<IReadOnlyList<AdminCourseCategoryOptionDto>> GetCategoryOptionsAsync(CancellationToken token = default)
    { var rows = await db.StudyCourseCategories.AsNoTracking().Select(c => new { c.Id, DisplayName = c.Translations.Where(t => t.LanguageCode == "fr").Select(t => t.Title).FirstOrDefault() ?? c.Translations.Where(t => t.LanguageCode == "en").Select(t => t.Title).FirstOrDefault() ?? "Catégorie sans traduction" }).ToListAsync(token); return rows.OrderBy(x => x.DisplayName).ThenBy(x => x.Id).Select(x => new AdminCourseCategoryOptionDto(x.Id, x.DisplayName)).ToList(); }

    public async Task<IReadOnlyList<AdminCoursePrerequisiteOptionDto>> GetPrerequisiteOptionsAsync(Guid? excludingCourseId = null, CancellationToken token = default) => await db.StudyCourses.AsNoTracking().Where(x => !excludingCourseId.HasValue || x.Id != excludingCourseId.Value).OrderBy(x => x.Title).Select(x => new AdminCoursePrerequisiteOptionDto(x.Id, x.Title, x.Slug, x.Level, x.CourseCategory.Title)).ToListAsync(token);

    public Task<string?> GetThumbnailAsync(Guid id, CancellationToken token = default) => db.StudyCourses.AsNoTracking().Where(x => x.Id == id).Select(x => x.Thumbnail).SingleOrDefaultAsync(token);
}