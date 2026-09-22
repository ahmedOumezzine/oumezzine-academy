using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Web.Services;
using System.Globalization;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class CourseCatalogTranslationTests
{
    [Fact]
    public async Task PublishedSlugResolvesOnlyInItsLanguage()
    {
        await using var db = CreateDatabase();
        var courseId = SeedCourse(db, includeEnglish: true);
        var service = CreateService(db);

        SetCulture("fr-FR");
        var french = await service.GetCourseAsync("apprendre-angular");
        SetCulture("en-US");
        var english = await service.GetCourseAsync("learn-angular");
        var wrongLanguage = await service.GetCourseAsync("apprendre-angular");

        Assert.Equal(courseId, french!.Id);
        Assert.Equal(courseId, english!.Id);
        Assert.Null(wrongLanguage);
    }

    [Fact]
    public async Task DraftEnglishTranslationIsNotVisible()
    {
        await using var db = CreateDatabase();
        SeedCourse(db, includeEnglish: false);
        db.StudyCourseTranslations.Add(new CourseTranslation { Id = Guid.NewGuid(), CourseId = db.StudyCourses.Single().Id, LanguageCode = "en", PublicationStatus = StudyStatus.Draft, Title = "Angular", Slug = "learn-angular" });
        await db.SaveChangesAsync();
        SetCulture("en-US");

        var result = await CreateService(db).GetCourseAsync("learn-angular");

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchUsesTheCurrentLanguageFields()
    {
        await using var db = CreateDatabase();
        SeedCourse(db, includeEnglish: true);
        SetCulture("en-US");

        var result = await CreateService(db).SearchCoursesAsync("Angular foundations", null, null, null, null, 1, 9);

        Assert.Single(result.Courses);
        Assert.Equal("Learn Angular", result.Courses[0].Title);
    }

    [Fact]
    public async Task SearchDoesNotMatchContentFromAnotherLanguage()
    {
        await using var db = CreateDatabase();
        SeedCourse(db, includeEnglish: true);
        var service = CreateService(db);

        SetCulture("fr-FR");
        var englishTermInFrench = await service.SearchCoursesAsync("Angular foundations", null, null, null, null, 1, 9);
        SetCulture("en-US");
        var frenchTermInEnglish = await service.SearchCoursesAsync("Fondations Angular", null, null, null, null, 1, 9);

        Assert.Empty(englishTermInFrench.Courses);
        Assert.Empty(frenchTermInEnglish.Courses);
    }

    [Fact]
    public async Task MissingEnglishTranslationIsNotVisible()
    {
        await using var db = CreateDatabase();
        SeedCourse(db, includeEnglish: false);
        SetCulture("en-US");

        var result = await CreateService(db).SearchCoursesAsync(null, null, null, null, null, 1, 9);

        Assert.Empty(result.Courses);
    }

    private static ApplicationDbContext CreateDatabase()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

    private static Guid SeedCourse(ApplicationDbContext db, bool includeEnglish)
    {
        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Frontend", Slug = "frontend", Status = StudyStatus.Published };
        category.Translations.Add(new CourseCategoryTranslation { Id = Guid.NewGuid(), LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Frontend", Slug = "frontend" });
        category.Translations.Add(new CourseCategoryTranslation { Id = Guid.NewGuid(), LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Frontend", Slug = "frontend" });
        var course = new Course { Id = Guid.NewGuid(), CourseCategory = category, Status = StudyStatus.Published, Level = StudyLevel.Beginner, CreatedOnUtc = DateTime.UtcNow, Title = "Legacy", Slug = "legacy" };
        course.Translations.Add(new CourseTranslation { Id = Guid.NewGuid(), LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Apprendre Angular", Slug = "apprendre-angular", Summary = "Fondations Angular" });
        if (includeEnglish) course.Translations.Add(new CourseTranslation { Id = Guid.NewGuid(), LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Learn Angular", Slug = "learn-angular", Summary = "Angular foundations" });
        db.StudyCourses.Add(course);
        db.SaveChanges();
        return course.Id;
    }

    private static CourseCatalogService CreateService(ApplicationDbContext db) => new(new EfCourseCatalogQueries(db), new CurrentLanguageService());

    private static void SetCulture(string name) => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
}