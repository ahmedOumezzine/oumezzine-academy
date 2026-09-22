using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Web.Services;
using OumezzineAcademy.Infrastructure.Sanitization;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class AdminContentServiceTests
{
    [Fact]
    public async Task Chapter_save_supports_published_french_and_missing_english()
    {
        await using var db = CreateDatabase();
        var (courseId, _) = await SeedCourseAsync(db);
        var service = new AdminChapterService(new EfAdminChapterQueries(db), new EfAdminChapterPersistence(db, new HtmlSanitizerService()), new NullStudyLmsCacheInvalidator(), new EfAdminChapterMediaCommands(db, new FileSystemMediaStorage(".")));

        await service.SaveAsync(new ChapterEditViewModel
        {
            CourseId = courseId, Order = 1,
            French = new() { Title = "Introduction", Summary = "Les bases", PublicationStatus = StudyStatus.Published }
        }, CancellationToken.None);

        var chapter = await db.StudyCourseContents.Include(x => x.Translations).SingleAsync();
        Assert.Equal(1, chapter.Order);
        Assert.Equal(StudyStatus.Published, chapter.Translations.Single().PublicationStatus);
        Assert.DoesNotContain(chapter.Translations, x => x.LanguageCode == "en");
    }

    [Fact]
    public async Task Chapter_reorder_normalizes_orders_and_delete_with_lessons_is_denied()
    {
        await using var db = CreateDatabase();
        var (courseId, categoryId) = await SeedCourseAsync(db);
        var first = new CourseContent { Id = Guid.NewGuid(), CourseId = courseId, Order = 1, Title = "A", CreatedOnUtc = DateTime.UtcNow };
        var second = new CourseContent { Id = Guid.NewGuid(), CourseId = courseId, Order = 2, Title = "B", CreatedOnUtc = DateTime.UtcNow };
        db.StudyCourseContents.AddRange(first, second);
        db.StudyCourseLessons.Add(new CourseLesson { Id = Guid.NewGuid(), CourseContentId = second.Id, Order = 1, Title = "Lesson", CreatedOnUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = new AdminChapterService(new EfAdminChapterQueries(db), new EfAdminChapterPersistence(db, new HtmlSanitizerService()), new NullStudyLmsCacheInvalidator(), new EfAdminChapterMediaCommands(db, new FileSystemMediaStorage(".")));

        await service.MoveAsync(courseId, second.Id, -1, CancellationToken.None);
        var ordered = await db.StudyCourseContents.Where(x => x.CourseId == courseId).OrderBy(x => x.Order).ToListAsync();
        Assert.Equal([second.Id, first.Id], ordered.Select(x => x.Id));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(courseId, second.Id, CancellationToken.None));
        Assert.Contains("leçons", error.Message);
        Assert.Equal(categoryId, (await db.StudyCourses.SingleAsync()).CourseCategoryId);
    }

    [Fact]
    public async Task Lesson_save_sanitizes_html_and_keeps_optional_media_nullable()
    {
        await using var db = CreateDatabase();
        var (courseId, _) = await SeedCourseAsync(db);
        var chapter = new CourseContent { Id = Guid.NewGuid(), CourseId = courseId, Order = 1, Title = "Chapter", CreatedOnUtc = DateTime.UtcNow };
        db.StudyCourseContents.Add(chapter);
        await db.SaveChangesAsync();
        var service = new AdminLessonService(new EfAdminLessonQueries(db), new EfAdminLessonCoreCommands(db, new HtmlSanitizerService()), new EfAdminLessonMediaCommands(db, new FileSystemMediaStorage(".")), new EfAdminLessonDeleteCommands(db), new NullStudyLmsCacheInvalidator());

        await service.SaveAsync(new LessonEditViewModel
        {
            CourseId = courseId, ChapterId = chapter.Id, Order = 1,
            French = new() { Title = "Texte", Slug = "texte", Summary = "Résumé", ContentHtml = "<h2>OK</h2><script>alert(1)</script><p onerror=bad>Texte</p>", PublicationStatus = StudyStatus.Published },
            English = new() { Title = "Text", Slug = "text", Summary = "Summary", ContentHtml = "<p>Safe</p>", PublicationStatus = StudyStatus.Draft }
        }, CancellationToken.None);

        var lesson = await db.StudyCourseLessons.Include(x => x.Translations).SingleAsync();
        var french = lesson.Translations.Single(x => x.LanguageCode == "fr");
        Assert.Contains("<h2>OK</h2>", french.ContentHtml);
        Assert.DoesNotContain("script", french.ContentHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", french.ContentHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Null(french.VideoUrl);
        Assert.Null(french.DocumentUrl);
        Assert.Equal(StudyStatus.Draft, lesson.Translations.Single(x => x.LanguageCode == "en").PublicationStatus);
    }

    [Fact]
    public async Task Lesson_reorder_delete_and_language_slug_check_are_supported()
    {
        await using var db = CreateDatabase();
        var (courseId, _) = await SeedCourseAsync(db);
        var chapter = new CourseContent { Id = Guid.NewGuid(), CourseId = courseId, Order = 1, Title = "Chapter", CreatedOnUtc = DateTime.UtcNow };
        var first = new CourseLesson { Id = Guid.NewGuid(), CourseContentId = chapter.Id, Order = 1, Title = "A", CreatedOnUtc = DateTime.UtcNow };
        var second = new CourseLesson { Id = Guid.NewGuid(), CourseContentId = chapter.Id, Order = 2, Title = "B", CreatedOnUtc = DateTime.UtcNow };
        db.StudyCourseContents.Add(chapter); db.StudyCourseLessons.AddRange(first, second);
        db.StudyCourseLessonTranslations.Add(new CourseLessonTranslation { Id = Guid.NewGuid(), CourseLessonId = first.Id, LanguageCode = "fr", Slug = "same", Title = "A" });
        await db.SaveChangesAsync();
        var service = new AdminLessonService(new EfAdminLessonQueries(db), new EfAdminLessonCoreCommands(db, new HtmlSanitizerService()), new EfAdminLessonMediaCommands(db, new FileSystemMediaStorage(".")), new EfAdminLessonDeleteCommands(db), new NullStudyLmsCacheInvalidator());

        Assert.True(await service.SlugExistsAsync("fr", "same", second.Id, CancellationToken.None));
        await service.MoveAsync(courseId, chapter.Id, second.Id, -1, CancellationToken.None);
        Assert.Equal(second.Id, (await db.StudyCourseLessons.OrderBy(x => x.Order).FirstAsync()).Id);
        await service.DeleteAsync(courseId, chapter.Id, second.Id, CancellationToken.None);
        Assert.Single(await db.StudyCourseLessons.ToListAsync());
    }

    private static ApplicationDbContext CreateDatabase()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

    private static async Task<(Guid CourseId, Guid CategoryId)> SeedCourseAsync(ApplicationDbContext db)
    {
        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Backend", Slug = "backend", CreatedOnUtc = DateTime.UtcNow };
        var course = new Course { Id = Guid.NewGuid(), CourseCategoryId = category.Id, CourseCategory = category, Title = "Course", Slug = "course", CreatedOnUtc = DateTime.UtcNow };
        db.StudyCourseCategories.Add(category); db.StudyCourses.Add(course); await db.SaveChangesAsync();
        return (course.Id, category.Id);
    }
}

