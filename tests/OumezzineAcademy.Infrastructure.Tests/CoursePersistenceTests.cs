using Xunit;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;

namespace OumezzineAcademy.Tests;

public sealed class CoursePersistenceTests
{
    [Fact]
    public async Task Saves_course_with_translations_and_rejects_duplicate_slug()
    {
        await using var db = CreateDb(out var categoryId);
        var commands = new EfAdminCourseCoreCommands(db);
        var result = await commands.SaveAsync(Command(null, categoryId, "course"));
        Assert.True(result.Success);
        Assert.Equal(2, await db.CourseTranslations.CountAsync());

        var duplicate = await commands.SaveAsync(Command(null, categoryId, "course"));
        Assert.False(duplicate.Success);
        Assert.Equal("Slug", duplicate.ErrorKey);
    }

    [Fact]
    public async Task Rejects_invalid_category_and_missing_course_edit()
    {
        await using var db = CreateDb(out var categoryId);
        var commands = new EfAdminCourseCoreCommands(db);
        var invalidCategory = await commands.SaveAsync(Command(null, Guid.NewGuid(), "invalid"));
        var missingCourse = await commands.SaveAsync(Command(Guid.NewGuid(), categoryId, "missing"));
        Assert.Equal("CategoryId", invalidCategory.ErrorKey);
        Assert.Equal("Cours introuvable.", missingCourse.ErrorMessage);
    }

    [Fact]
    public async Task Synchronizes_prerequisites_and_rejects_self_reference()
    {
        await using var db = CreateDb(out var categoryId);
        var course = new Course { Id = Guid.NewGuid(), CourseCategoryId = categoryId, Title = "Other" };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        var commands = new EfAdminCoursePrerequisiteCommands(db, new Validator(false));
        var owner = (await db.Courses.Select(x => x.Id).ToListAsync()).First();
        var result = await commands.SynchronizeAsync(owner, new[] { course.Id, course.Id });
        Assert.True(result.Success);
        Assert.Single(await db.CoursePrerequisites.ToListAsync());
        var self = await commands.SynchronizeAsync(owner, new[] { owner });
        Assert.False(self.Success);
    }

    private static ApplicationDbContext CreateDb(out Guid categoryId)
    {
        categoryId = Guid.NewGuid();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.CourseCategories.Add(new CourseCategory { Id = categoryId, Title = "Category" });
        db.Courses.Add(new Course { Id = Guid.NewGuid(), CourseCategoryId = categoryId, Title = "Owner" });
        db.SaveChanges();
        return db;
    }

    private static AdminCourseCoreSaveCommand Command(Guid? id, Guid categoryId, string slug)
        => new(id, categoryId, StudyLevel.Beginner, null,
            new("Cours", slug, null, null, null, null, null, null, null, StudyStatus.Published),
            new("Course", slug + "-en", null, null, null, null, null, null, null, StudyStatus.Published));

    private sealed class Validator(bool value) : ICoursePrerequisiteValidator
    {
        public Task<bool> WouldCreateCycleAsync(Guid courseId, IEnumerable<Guid> prerequisites, CancellationToken cancellationToken = default) => Task.FromResult(value);
    }
}
