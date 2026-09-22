using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Web.Services;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class AdminCourseCategoryOptionTests
{
    [Fact]
    public async Task Saving_an_existing_course_syncs_root_status_with_french_publication_status()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var category = Category("backend");
        var course = new Course
        {
            Id = Guid.NewGuid(), CourseCategoryId = category.Id, CourseCategory = category,
            Title = "Course", Slug = "course", CreatedOnUtc = DateTime.UtcNow,
            Status = StudyStatus.Draft
        };
        course.Translations.Add(new CourseTranslation
        {
            Id = Guid.NewGuid(), CourseId = course.Id, LanguageCode = "fr",
            Title = "Course", Slug = "course", PublicationStatus = StudyStatus.Draft
        });
        db.StudyCourseCategories.Add(category);
        db.StudyCourses.Add(course);
        await db.SaveChangesAsync();

        var save = await new EfAdminCourseCoreCommands(db).SaveAsync(new AdminCourseCoreSaveCommand(course.Id, category.Id, StudyLevel.Beginner, null,
            new("Course publiée", "course-publiee", null, null, null, null, null, null, null, StudyStatus.Published),
            new(null, null, null, null, null, null, null, null, null, StudyStatus.Draft)), CancellationToken.None);
        Assert.True(save.Success);

        Assert.Equal(StudyStatus.Published, (await db.StudyCourses.SingleAsync()).Status);
    }

    [Fact]
    public async Task Category_options_use_french_then_english_then_missing_and_sort_by_display_name()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var french = Category("z-backend");
        french.Translations.Add(Translation(french.Id, "fr", "Backend"));
        var englishOnly = Category("a-data");
        englishOnly.Translations.Add(Translation(englishOnly.Id, "en", "Data"));
        var missing = Category("b-missing");
        db.StudyCourseCategories.AddRange(french, englishOnly, missing);
        await db.SaveChangesAsync();

        var options = await new EfAdminCourseQueries(db).GetCategoryOptionsAsync(CancellationToken.None);

        Assert.Equal(["Backend", "Catégorie sans traduction", "Data"], options.Select(x => x.DisplayName));
        Assert.Equal(englishOnly.Id, options[2].Id);
        Assert.Equal(missing.Id, options[1].Id);
    }

    private static CourseCategory Category(string slug) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Title = slug,
        CreatedOnUtc = DateTime.UtcNow
    };

    private static CourseCategoryTranslation Translation(Guid categoryId, string language, string title) => new()
    {
        Id = Guid.NewGuid(),
        CourseCategoryId = categoryId,
        LanguageCode = language,
        Title = title,
        Slug = title.ToLowerInvariant()
    };
}

