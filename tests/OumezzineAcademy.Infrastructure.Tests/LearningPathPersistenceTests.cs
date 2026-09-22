using Xunit;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Infrastructure.Sanitization;

namespace OumezzineAcademy.Tests;

public sealed class LearningPathPersistenceTests
{
    [Fact]
    public async Task Saves_learning_path_and_translations()
    {
        await using var db = CreateDb(out var categoryId, out _, out _);
        var result = await new EfAdminLearningPathCoreCommands(db, new HtmlSanitizerService()).SaveAsync(Command(null, categoryId));
        Assert.True(result.Success);
        Assert.Equal(2, await db.LearningPathTranslations.CountAsync());
    }

    [Fact]
    public async Task Composes_courses_and_rejects_duplicates_or_missing_items()
    {
        await using var db = CreateDb(out var categoryId, out var pathId, out var courseId);
        var commands = new EfAdminLearningPathCommands(db);
        await commands.AddCourseAsync(pathId, courseId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => commands.AddCourseAsync(pathId, courseId));
        await Assert.ThrowsAsync<InvalidOperationException>(() => commands.SynchronizeCoursesAsync(pathId, new[] { Guid.NewGuid() }));
        Assert.Single(await db.LearningPathCourses.ToListAsync());
    }

    [Fact]
    public async Task Deletes_learning_path_and_rejects_unknown_path()
    {
        await using var db = CreateDb(out _, out var pathId, out _);
        var commands = new EfAdminLearningPathCommands(db);
        await commands.DeleteAsync(pathId);
        Assert.Empty(await db.LearningPaths.ToListAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => commands.DeleteAsync(Guid.NewGuid()));
    }

    private static ApplicationDbContext CreateDb(out Guid categoryId, out Guid pathId, out Guid courseId)
    {
        categoryId = Guid.NewGuid(); pathId = Guid.NewGuid(); courseId = Guid.NewGuid();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var category = new LearningPathCategory { Id = categoryId, Title = "Category", Slug = "category" };
        db.LearningPathCategories.Add(category);
        db.LearningPaths.Add(new LearningPath { Id = pathId, LearningPathCategoryId = categoryId, Title = "Path", Slug = "path" });
        var courseCategory = new CourseCategory { Id = Guid.NewGuid(), Title = "Course category" };
        db.Courses.Add(new Course { Id = courseId, CourseCategory = courseCategory, Title = "Course", Slug = "course" });
        db.SaveChanges();
        return db;
    }

    private static AdminLearningPathSaveCommand Command(Guid? id, Guid categoryId)
        => new(id, categoryId, StudyLevel.Beginner, null,
            new("Parcours", "parcours", "Résumé", null, null, StudyStatus.Published),
            new("Path", "path-en", "Summary", null, null, StudyStatus.Published));
}
