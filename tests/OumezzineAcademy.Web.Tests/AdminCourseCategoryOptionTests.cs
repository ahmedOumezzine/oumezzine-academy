using AhmedOumezzine.EFCore.Repository.Extensions;
using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
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
            Id = Guid.NewGuid(),
            CourseCategoryId = category.Id,
            CourseCategory = category,
            Title = "Course",
            Slug = "course",
            CreatedOnUtc = DateTime.UtcNow,
            Status = StudyStatus.Draft
        };
        course.Translations.Add(new CourseTranslation
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            LanguageCode = "fr",
            Title = "Course",
            Slug = "course",
            PublicationStatus = StudyStatus.Draft
        });
        db.StudyCourseCategories.Add(category);
        db.StudyCourses.Add(course);
        await db.SaveChangesAsync();

        var save = await new EfAdminCourseCoreCommands(CreateRepository(db)).SaveAsync(new AdminCourseCoreSaveCommand(course.Id, category.Id, StudyLevel.Beginner, null,
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

        var options = await new EfAdminCourseQueries(CreateRepository(db)).GetCategoryOptionsAsync(CancellationToken.None);

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

    private static IRepository CreateRepository(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        services.AddGenericRepository<ApplicationDbContext>();
        return services.BuildServiceProvider().GetRequiredService<IRepository>();
    }

    private static CourseCategoryTranslation Translation(Guid categoryId, string language, string title) => new()
    {
        Id = Guid.NewGuid(),
        CourseCategoryId = categoryId,
        LanguageCode = language,
        Title = title,
        Slug = title.ToLowerInvariant()
    };
}
