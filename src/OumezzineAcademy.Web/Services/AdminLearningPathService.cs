using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;

namespace OumezzineAcademy.Web.Services;

public sealed class AdminLearningPathService
{
    private readonly IAdminLearningPathMediaCommands _media;
    private readonly IStudyLmsCacheInvalidator _cache;
    private readonly IAdminLearningPathQueries _queries;
    private readonly IAdminLearningPathCoreCommands _core;
    private readonly IAdminLearningPathCommands _composition;

    public AdminLearningPathService(IAdminLearningPathMediaCommands media, IAdminLearningPathQueries queries, IAdminLearningPathCoreCommands core, IAdminLearningPathCommands composition, IStudyLmsCacheInvalidator? cache = null)
    { _media = media; _queries = queries; _core = core; _composition = composition; _cache = cache ?? new NullStudyLmsCacheInvalidator(); }

    public async Task<AdminLearningPathsListViewModel> ListAsync(CancellationToken token) => new() { Items = (await _queries.ListAsync(token)).Select(x => new AdminLearningPathListItem { Id = x.Id, Title = x.Title, CategoryTitle = x.CategoryTitle, Level = x.Level, FrenchStatus = x.FrenchStatus, EnglishStatus = x.EnglishStatus, CourseCount = x.CourseCount }).ToList() };

    public async Task<LearningPathEditViewModel?> GetFormAsync(Guid? id, CancellationToken token)
    {
        var e = await _queries.GetForEditAsync(id, token); if (e is null) return null; static LearningPathTranslationInput M(AdminLearningPathTranslationDto x) => new() { Title = x.Title, Slug = x.Slug, Summary = x.Summary, MetaTitle = x.MetaTitle, MetaDescription = x.MetaDescription, PublicationStatus = x.PublicationStatus }; return new() { Id = e.Id, CategoryId = e.CategoryId, Level = e.Level, ThumbnailUrl = e.Thumbnail, Categories = e.Categories.Select(x => new AdminLearningPathCategoryOption { Id = x.Id, Title = x.Title }).ToList(), SelectedCategoryTitle = e.SelectedCategoryTitle, French = M(e.French), English = M(e.English) };
    }

    public Task<bool> SlugExistsAsync(string language, string slug, Guid id, CancellationToken token) => _queries.SlugExistsAsync(language, slug, id, token);

    public async Task<Guid> SaveAsync(LearningPathEditViewModel model, CancellationToken token)
    {
        static AdminLearningPathTranslationDto M(LearningPathTranslationInput x) => new(x.Title, x.Slug, x.Summary, x.MetaTitle, x.MetaDescription, x.PublicationStatus); var r = await _core.SaveAsync(new AdminLearningPathSaveCommand(model.Id == Guid.Empty ? null : model.Id, model.CategoryId, model.Level, model.ThumbnailUrl, M(model.French), M(model.English)), token); if (!r.Success) throw new InvalidOperationException(r.Error); if (model.Thumbnail is not null) { await using var stream = model.Thumbnail.OpenReadStream(); await _media.UploadAsync(r.LearningPathId, new MediaUpload(model.Thumbnail.FileName, model.Thumbnail.ContentType, model.Thumbnail.Length, stream), token); }
        await _cache.InvalidatePublicAsync(token); return r.LearningPathId;
    }

    public async Task DeleteAsync(Guid id, CancellationToken token)
    { await _composition.DeleteAsync(id, token); await _cache.InvalidatePublicAsync(token); }

    public async Task<AdminLearningPathCoursesViewModel?> CoursesAsync(Guid id, CancellationToken token)
    {
        var x = await _queries.GetCompositionAsync(id, token); if (x is null) return null; var selected = x.SelectedCourses.Select((c, i) => new AdminLearningPathCourseItem { Id = c.LinkId, CourseId = c.CourseId, Order = c.Order, Title = c.Title, CategoryTitle = c.CategoryName, Level = c.Level, FrenchStatus = c.FrenchStatus, EnglishStatus = c.EnglishStatus, IsFirst = i == 0, IsLast = i == x.SelectedCourses.Count - 1 }).ToList(); var available = x.AvailableCourses.Select(c => new AdminCourseOption { Id = c.CourseId, Title = c.Title, Slug = c.Slug, Level = c.Level, CategoryName = c.CategoryName, FrenchStatus = c.FrenchStatus, EnglishStatus = c.EnglishStatus }).ToList(); return new() { LearningPathId = x.LearningPathId, LearningPathTitle = x.LearningPathTitle, SelectedCourses = selected, AvailableCourses = available };
    }

    public async Task AddCourseAsync(Guid pathId, Guid courseId, CancellationToken token)
    { await _composition.AddCourseAsync(pathId, courseId, token); await _cache.InvalidatePublicAsync(token); }

    public async Task SynchronizeCoursesAsync(Guid pathId, IReadOnlyCollection<Guid> courseIds, CancellationToken token)
    { await _composition.SynchronizeCoursesAsync(pathId, courseIds, token); await _cache.InvalidatePublicAsync(token); }

    public async Task MoveCourseAsync(Guid pathId, Guid id, int direction, CancellationToken token)
    { await _composition.MoveCourseAsync(pathId, id, direction, token); await _cache.InvalidatePublicAsync(token); }

    public async Task RemoveCourseAsync(Guid pathId, Guid id, CancellationToken token)
    { await _composition.RemoveCourseAsync(pathId, id, token); await _cache.InvalidatePublicAsync(token); }
}