using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Infrastructure.Sanitization;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class ChapterPersistenceTests
{
    [Fact]
    public async Task Saves_new_chapter_and_normalizes_order()
    {
        await using var db = CreateDb(out var courseId);
        var persistence = new EfAdminChapterPersistence(db, new HtmlSanitizerService());

        var result = await persistence.SaveAsync(Command(null, courseId, 9));

        Assert.True(result.Success);
        Assert.Equal(1, await db.CourseContents.CountAsync());
        Assert.Equal(1, await db.CourseContents.Select(x => x.Order).SingleAsync());
    }

    [Fact]
    public async Task Returns_error_for_unknown_course_and_chapter()
    {
        await using var db = CreateDb(out var courseId);
        var persistence = new EfAdminChapterPersistence(db, new HtmlSanitizerService());
        var missingCourse = await persistence.SaveAsync(Command(null, Guid.NewGuid(), 1));
        var missingChapter = await persistence.SaveAsync(Command(Guid.NewGuid(), courseId, 1));

        Assert.Equal("Course not found.", missingCourse.Error);
        Assert.Equal("Chapter not found.", missingChapter.Error);
    }

    [Fact]
    public async Task Deletes_empty_chapter_but_rejects_chapter_with_lesson()
    {
        await using var db = CreateDb(out var courseId);
        var chapter = new CourseContent { Id = Guid.NewGuid(), CourseId = courseId, Title = "Chapter", Order = 1 };
        db.CourseContents.Add(chapter);
        await db.SaveChangesAsync();
        var persistence = new EfAdminChapterPersistence(db, new HtmlSanitizerService());

        var deleted = await persistence.DeleteAsync(courseId, chapter.Id);
        Assert.True(deleted.Success);

        var blocked = new CourseContent { Id = Guid.NewGuid(), CourseId = courseId, Title = "Blocked", Order = 1 };
        blocked.CourseLessons.Add(new CourseLesson { Id = Guid.NewGuid(), Title = "Lesson", Slug = "lesson", Order = 1 });
        db.CourseContents.Add(blocked);
        await db.SaveChangesAsync();
        var rejected = await persistence.DeleteAsync(courseId, blocked.Id);
        Assert.False(rejected.Success);
        Assert.Contains("leçons", rejected.Message);
    }

    private static ApplicationDbContext CreateDb(out Guid courseId)
    {
        courseId = Guid.NewGuid();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Category" };
        db.CourseCategories.Add(category);
        db.Courses.Add(new Course { Id = courseId, Title = "Course", CourseCategoryId = category.Id });
        db.SaveChanges();
        return db;
    }

    private static AdminChapterSaveCommand Command(Guid? id, Guid courseId, int order)
        => new(id, courseId, order, new("Chapitre", "Résumé", StudyStatus.Published), new("Chapter", null, StudyStatus.Published));
}