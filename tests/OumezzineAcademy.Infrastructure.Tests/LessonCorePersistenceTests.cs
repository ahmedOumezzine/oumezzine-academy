using AhmedOumezzine.EFCore.Repository.Extensions;
using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Infrastructure.Sanitization;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class LessonCorePersistenceTests
{
    [Fact]
    public async Task Saves_new_lesson_and_sanitizes_content()
    {
        await using var db = CreateDb(out var courseId, out var chapterId);
        var commands = new EfAdminLessonCoreCommands(db, new HtmlSanitizerService(), CreateRepository(db));

        var result = await commands.SaveAsync(Command(null, courseId, chapterId, 5, "<p>ok</p><script>bad()</script>"));

        Assert.True(result.Success);
        var saved = await db.CourseLessons.Include(x => x.Translations).SingleAsync();
        Assert.Equal(1, saved.Order);
        Assert.DoesNotContain("script", saved.Translations.Single(x => x.LanguageCode == "fr").ContentHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Returns_errors_for_missing_chapter_and_lesson()
    {
        await using var db = CreateDb(out var courseId, out var chapterId);
        var commands = new EfAdminLessonCoreCommands(db, new HtmlSanitizerService(), CreateRepository(db));
        var missingChapter = await commands.SaveAsync(Command(null, courseId, Guid.NewGuid(), 1, null));
        var missingLesson = await commands.SaveAsync(Command(Guid.NewGuid(), courseId, chapterId, 1, null));

        Assert.Equal("Chapter not found.", missingChapter.Error);
        Assert.Equal("Lesson not found.", missingLesson.Error);
    }

    [Fact]
    public async Task Moves_lesson_and_preserves_contiguous_order()
    {
        await using var db = CreateDb(out var courseId, out var chapterId);
        var first = new CourseLesson { Id = Guid.NewGuid(), CourseContentId = chapterId, Title = "First", Slug = "first", Order = 1 };
        var second = new CourseLesson { Id = Guid.NewGuid(), CourseContentId = chapterId, Title = "Second", Slug = "second", Order = 2 };
        db.CourseLessons.AddRange(first, second);
        await db.SaveChangesAsync();
        await new EfAdminLessonCoreCommands(db, new HtmlSanitizerService(), CreateRepository(db)).MoveAsync(courseId, chapterId, second.Id, -1);
        db.ChangeTracker.Clear();

        var orders = await db.CourseLessons.OrderBy(x => x.Order).Select(x => x.Id).ToListAsync();
        Assert.Equal([second.Id, first.Id], orders);
    }

    private static ApplicationDbContext CreateDb(out Guid courseId, out Guid chapterId)
    {
        courseId = Guid.NewGuid();
        chapterId = Guid.NewGuid();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Category" };
        db.Courses.Add(new Course { Id = courseId, Title = "Course", CourseCategoryId = category.Id, CourseCategory = category });
        db.CourseContents.Add(new CourseContent { Id = chapterId, CourseId = courseId, Title = "Chapter", Order = 1 });
        db.SaveChanges();
        return db;
    }

    private static IRepository CreateRepository(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        services.AddGenericRepository<ApplicationDbContext>();
        return services.BuildServiceProvider().GetRequiredService<IRepository>();
    }

    private static AdminLessonSaveCommand Command(Guid? id, Guid courseId, Guid chapterId, int order, string? html)
        => new(id, courseId, chapterId, order, 10,
            new("Lesson", "lesson", null, html, null, null, null, null, StudyStatus.Published),
            new("Lesson", "lesson-en", null, html, null, null, null, null, StudyStatus.Published));
}
