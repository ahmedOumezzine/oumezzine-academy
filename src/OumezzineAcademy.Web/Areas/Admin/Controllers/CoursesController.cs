using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class CoursesController : Controller
{
    private readonly AdminCourseService _service;
    private readonly IAdminCourseQueries _queries;

    public CoursesController(AdminCourseService service, IAdminCourseQueries queries)
    {
        _service = service;
        _queries = queries;
    }

    public async Task<IActionResult> Index(string? search, Guid? category, StudyLevel? level, string? frStatus, string? enStatus, string? sort, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is 20 or 50 ? pageSize : 10;
        var categories = (await _queries.GetCategoryOptionsAsync(cancellationToken)).Select(x => new AdminCategoryOption { Id = x.Id, DisplayName = x.DisplayName }).ToList();
        var result = await _queries.ListAsync(search, category, level, frStatus, enStatus, sort, page, pageSize, cancellationToken);
        var items = result.Items.Select(x => new AdminCourseListItem { Id = x.Id, CourseStatus = x.CourseStatus, Thumbnail = x.Thumbnail, Slug = x.Slug, DisplayTitle = x.DisplayTitle, FrenchTitle = x.FrenchTitle, EnglishTitle = x.EnglishTitle, CategoryTitle = x.CategoryTitle, Level = x.Level, FrenchStatus = x.FrenchStatus, EnglishStatus = x.EnglishStatus, LessonsCount = x.LessonsCount, QuizzesCount = x.QuizzesCount, DateUtc = x.DateUtc, FrenchVisibility = CourseVisibilityPolicy.Evaluate(x.CourseStatus, x.FrenchStatus, "fr"), EnglishVisibility = CourseVisibilityPolicy.Evaluate(x.CourseStatus, x.EnglishStatus, "en") }).ToList();
        return View(new AdminCoursesListViewModel { Items = items, TotalCount = result.TotalCount, Categories = categories, Search = search, Category = category, Level = level, FrenchStatus = frStatus, EnglishStatus = enStatus, Sort = string.IsNullOrWhiteSpace(sort) ? "modified" : sort, Page = page, PageSize = pageSize });
    }

    [HttpGet] public async Task<IActionResult> Create(CancellationToken token) => View(new CourseEditViewModel { Categories = await CategoryOptionsAsync(token), AvailablePrerequisites = await PrerequisitesAsync(token) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseEditViewModel model, CancellationToken token) => await Save(model, token);

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken token)
    {
        var e = await _queries.GetForEditAsync(id, token);
        if (e is null) return NotFound();
        return View("Create", new CourseEditViewModel { Id = e.Id, CategoryId = e.CategoryId, Level = e.Level, ThumbnailUrl = NormalizeThumbnailUrl(e.Thumbnail), Categories = await CategoryOptionsAsync(token), AvailablePrerequisites = await PrerequisitesAsync(token), PrerequisiteCourseIds = e.PrerequisiteCourseIds.ToList(), French = Input(e.French), English = Input(e.English) });
    }

    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Edit(CourseEditViewModel model, CancellationToken token) => await Save(model, token);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        if (!ModelState.IsValid || id == Guid.Empty) return NotFound();
        var result = await _service.DeleteAsync(id, token);
        if (result.NotFound) return NotFound();
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(CourseEditViewModel model, CancellationToken token)
    {
        var service = _service;
        Validate(model.French, "French"); Validate(model.English, "English");
        if (!string.IsNullOrWhiteSpace(model.French.Slug) && await service.SlugExistsAsync("fr", model.French.Slug, model.Id, token)) ModelState.AddModelError("French.Slug", "Ce slug existe déjà en français.");
        if (!string.IsNullOrWhiteSpace(model.English.Slug) && await service.SlugExistsAsync("en", model.English.Slug, model.Id, token)) ModelState.AddModelError("English.Slug", "Ce slug existe déjà en anglais.");
        if (!ModelState.IsValid)
        {
            await PrepareFormModelAsync(model, token);
            return View("Create", model);
        }
        try
        {
            var result = await service.SaveWithThumbnailAsync(model, token);
            foreach (var error in result.Errors) ModelState.AddModelError(error.Key, error.Message);
            if (!result.Success) { await PrepareFormModelAsync(model, token); return View("Create", model); }
            TempData["Success"] = "Cours enregistré.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError("Thumbnail", ex.Message); await PrepareFormModelAsync(model, token);
            return View("Create", model);
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError("", "Impossible d'enregistrer ce cours. Vérifiez les valeurs saisies."); await PrepareFormModelAsync(model, token);
            return View("Create", model);
        }
    }

    private async Task<IReadOnlyList<AdminCategoryOption>> CategoryOptionsAsync(CancellationToken token) => (await _queries.GetCategoryOptionsAsync(token)).Select(x => new AdminCategoryOption { Id = x.Id, DisplayName = x.DisplayName }).ToList();

    private async Task<List<AdminCourseOption>> PrerequisitesAsync(CancellationToken token) => (await _queries.GetPrerequisiteOptionsAsync()).Select(x => new AdminCourseOption { Id = x.Id, Title = x.Title, Slug = x.Slug, Level = x.Level, CategoryName = x.CategoryName }).ToList();

    private async Task PrepareFormModelAsync(CourseEditViewModel model, CancellationToken token)
    {
        model.Categories = await CategoryOptionsAsync(token);
        model.AvailablePrerequisites = await PrerequisitesAsync(token);
        if (model.Id != Guid.Empty && string.IsNullOrWhiteSpace(model.ThumbnailUrl))
            model.ThumbnailUrl = NormalizeThumbnailUrl(await _service.GetThumbnailAsync(model.Id, token));
    }

    private static string? NormalizeThumbnailUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().Replace('\\', '/');
        var wwwrootIndex = normalized.IndexOf("/wwwroot/", StringComparison.OrdinalIgnoreCase);
        if (wwwrootIndex >= 0) normalized = normalized[(wwwrootIndex + "/wwwroot".Length)..];
        if (normalized.StartsWith("~/", StringComparison.Ordinal)) normalized = normalized[1..];
        return normalized.StartsWith("/", StringComparison.Ordinal) ? normalized : "/" + normalized;
    }

    private void Validate(CourseTranslationInput input, string language)
    {
        if (string.IsNullOrWhiteSpace(input.Title) != string.IsNullOrWhiteSpace(input.Slug)) ModelState.AddModelError(language, "Le titre et le slug doivent être renseignés ensemble.");
        if (input.PublicationStatus == StudyStatus.Published && (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Slug) || string.IsNullOrWhiteSpace(input.Summary))) ModelState.AddModelError(language, "Une traduction publiée doit avoir titre, slug et résumé.");
    }

    private static CourseTranslationInput Input(OumezzineAcademy.Domain.Catalog.CourseTranslation? x) => x is null ? new() : new() { Title = x.Title, Slug = x.Slug, Summary = x.Summary, Overview = x.Overview, WhatYouLearn = x.WhatYouLearn, Requirements = x.Requirements, Audience = x.Audience, MetaTitle = x.MetaTitle, MetaDescription = x.MetaDescription, PublicationStatus = x.PublicationStatus };

    private static CourseTranslationInput Input(AdminCourseTranslationDto x) => new() { Title = x.Title, Slug = x.Slug, Summary = x.Summary, Overview = x.Overview, WhatYouLearn = x.WhatYouLearn, Requirements = x.Requirements, Audience = x.Audience, MetaTitle = x.MetaTitle, MetaDescription = x.MetaDescription, PublicationStatus = x.PublicationStatus };

    [HttpPost("thumbnail/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadThumbnail(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var url = await _service.UploadThumbnailAsync(id, file, cancellationToken);
            return url is null ? NotFound() : Ok(new { url });
        }
        catch (InvalidDataException exception)
        {
            ModelState.AddModelError(nameof(file), exception.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("thumbnail/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteThumbnail(Guid id, CancellationToken cancellationToken)
    {
        return await _service.DeleteThumbnailAsync(id, cancellationToken) ? Ok() : NotFound();
    }
}